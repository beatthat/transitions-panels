using UnityEditor;
using UnityEngine;

using System.Collections.Generic;

using BeatThat.Anim;
using BeatThat.UI;
using System;

namespace BeatThat.UI.Comp
{
	[CustomEditor(typeof(TransitionsPanel))]
	public class TransitionsPanelEditor : UnityEditor.Editor
	{
		private const float COL_WIDTH_TRANSITION_NAME = 50f;
		private const float COL_WIDTH_TRANSITION_TYPE = 80f;
		private const float COL_WIDTH_ACTION = 50f;
		
		public override void OnInspectorGUI()
		{
			
			base.OnInspectorGUI();
			
			GUILayout.BeginHorizontal();
			
			GUILayout.Label("Transitions", GUILayout.MaxWidth(80f));
			
			DisplayAddTransition("new-t");
		
			GUILayout.EndHorizontal();
			
			Dictionary<string, Transition[]> transitionsById = 
				TransitionUtils.FindAndGroupTransitions(this.panel.transform);
			
			foreach(KeyValuePair<string, Transition[]> t in transitionsById) {
				DisplayTransitionType(t.Key, t.Value);
			}
			
			DisplayIfMissing(transitionsById, PanelTransition.IN.name, PanelTransition.OUT.name);
			
			GUI.contentColor = Color.white;
			
			if(m_doOrganizeInspector) {
				m_doOrganizeInspector = false;
				OrganizeInspector();
			}
		}
		
		/// <summary>
		/// Had been working but suddenly EditorUtility.CopySerialized is crashing unity. Will recheck in Unity 4...
		/// </summary>
		private void OrganizeInspector()
		{
		}

		private Panel panel
		{
			get {
				return target as Panel;
			}
		}
		
		private void DisplayIfMissing(Dictionary<string, Transition[]> transitionsById, params string[] names)
		{
			foreach(string n in names) {
				Transition[] tlist;
				if(!transitionsById.TryGetValue(n, out tlist) || tlist == null || tlist.Length == 0) {
					DisplayTransitionType(n, null);
				}
			}
		}
		
		private void DisplayTransitionType(string name, Transition[] transitions)
		{
			if(string.IsNullOrEmpty(name)) {
				return;
			}
			
			GUILayout.BeginHorizontal();
			
			if(transitions == null) {
				GUI.contentColor = Color.red; 
			}
			else {
				GUI.contentColor = Color.white;
			}
			

			// label for the transition type
			GUILayout.BeginVertical();
			GUILayout.Label(name, GUILayout.Width(COL_WIDTH_TRANSITION_NAME));
			GUILayout.EndVertical();
			
			// list of transitions
			GUILayout.BeginVertical();
			
			if(string.IsNullOrEmpty(name)) {
				DisplayNoNameTransition();
			}
			else if(transitions != null) {
				foreach(Transition t in transitions) {
					DisplayTransition(t);
				}
			}
			else {
				DisplayMissingTransition(name);
			}
			GUILayout.EndVertical();
			
			GUILayout.EndHorizontal();
		}
		
		private void DisplayMissingTransition(string name)
		{
			GUILayout.BeginHorizontal();
			DisplayTransitionType("missing");
			DisplayAddTransition(name);
			GUILayout.EndHorizontal();
		}
		
		private void DisplayNoNameTransition()
		{
			GUILayout.BeginHorizontal();
			DisplayTransitionType("no name");
			GUILayout.EndHorizontal();
		}
		
		private void DisplayTransition(Transition t)
		{
			GUILayout.BeginHorizontal();
			
			DisplayTransitionType(TypeDisplayName(t));
			
			GUILayout.FlexibleSpace();
			
			DisplayTransitionAction("Delete", () => { 
				DestroyImmediate(t as Component);
				EditorGUIUtility.ExitGUI(); // have to kill this draw iteration because we're destroying a sibling game object
			});
			
			GUILayout.EndHorizontal();
		}
		
		private void DisplayTransitionType(string type)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label(type, GUILayout.Width(COL_WIDTH_TRANSITION_TYPE));
			GUILayout.EndHorizontal();
		}
		
		private void DisplayTransitionAction(string label, System.Action a)
		{
			GUILayout.BeginHorizontal();
			if(GUILayout.Button(label, GUILayout.Width(COL_WIDTH_ACTION))) {
				a();
			}
			GUILayout.EndHorizontal();
		}
		
		
		private void DisplayAddTransition(string tname)
		{	
			GUI.contentColor = Color.white;
					
			AddTransitionCommand[] opts = this.addTransitionOpts;
			
			System.Func<string[]> getOptNames = () => {
				List<string> tmp = new List<string>();
				foreach(AddTransitionCommand c in opts) {
					tmp.Add(c.displayName);
				}
				return tmp.ToArray();
			};
			
			string[] optNames = getOptNames();
			
			GUILayout.BeginHorizontal();
			
			GUILayout.FlexibleSpace();
			
			int selectedIx;
			if(!m_typeSelectionsByTransitionName.TryGetValue(tname, out selectedIx)) {
				selectedIx = 0;
			}
			
			selectedIx = EditorGUILayout.Popup("", selectedIx, optNames, GUILayout.MaxWidth(50f));
			
			m_typeSelectionsByTransitionName[tname] = selectedIx;
			
			bool doAdd = GUILayout.Button("Add", GUILayout.MaxWidth(COL_WIDTH_ACTION));
			
			if(doAdd) {
				opts[selectedIx].AddTransition(tname, this.panel);
				
				EditorUtility.SetDirty(target);
			}
			
			GUILayout.EndHorizontal();
			
		}
		
		virtual protected AddTransitionCommand[] addTransitionOpts
		{
			get {
				if(m_addTransitionOpts == null) {
					var noArgTypes = new Type[0];
					var noArgs = new object[0];
					using(var cmds = ListPool<AddTransitionCommand>.Get()) {
	
						var cmdTypes = TypeUtils.FindTypesByAssignableType<AddTransitionCommand>();
						foreach(var c in cmdTypes) {
							try {
								var curCmd = c.GetConstructor(noArgTypes).Invoke(noArgs) as AddTransitionCommand;
								if(curCmd != null) {
									cmds.Add(curCmd);
								}
							}
							catch(Exception e) {
								Debug.LogError(e);
							}
						}
//						cmds.Add(new AddAnimTransition());
//						cmds.Add(new AddTweenFromToTransition());
//						cmds.Add(new AddTweenToTransition());
//						cmds.Add(new AddFadeCanvasTransition());
						
						m_addTransitionOpts = cmds.ToArray();
					}
				}
				return m_addTransitionOpts;
			}
		}
		
		private bool TryAddTransitionOpt(string className, List<AddTransitionCommand> cmds)
		{
			try {
				Type t = Type.GetType(className);
				if(t != null) {
					AddTransitionCommand c = t.GetConstructor(new Type[0]).Invoke(new object[0]) 
						as AddTransitionCommand;
					
					if(c != null) {
						cmds.Add(c);
						return true;
					}
					else {
						Debug.LogWarning("Failed to instantiate type " + t.FullName);
					}
				}
				else {
					Debug.LogWarning ("Type '" + className + "' not found");
				}
			}
			catch(System.Exception e) {
				Debug.LogWarning("Failed to add transition opt with class name '" 
					+ className + "': " + e.Message);
				
			}
			return false;
//    	MethodInfo method 
//             = t.GetMethod("Bar", BindingFlags.Static | BindingFlags.Public);
//
//    	method.Invoke(null, null);
		}
		
		public interface AddTransitionCommand
		{
			string displayName
			{
				get;
			}
			
			void AddTransition(string tname, Panel panel);
		}
		
		
		private string TypeDisplayName(Transition t)
		{
			if(t.GetType().Name == "AnimTransition") {
				return "anim";
			}
			else {
				return (t != null)? t.GetType().Name: "";
			}
		}

		private AddTransitionCommand[] m_addTransitionOpts;
		private Dictionary<string, int> m_typeSelectionsByTransitionName = new Dictionary<string, int>();
		private bool m_doOrganizeInspector;
	}
	
}
