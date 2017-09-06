using System.Collections.Generic;
using BeatThat.Anim;
using BeatThat.UI;
using UnityEngine;

namespace BeatThat.UI
{	
	/// <summary>
	/// Standalone POCO implementation of Panel (a panel that has transitions).
	/// 
	/// Generally, this will be wrapped by a MonoBehaviour implementation that can attached to a GameObject
	/// </summary>
	public class PocoTransitionsPanel : Panel
	{
		public PocoTransitionsPanel(Transform t)
		{
			this.transform = t;
		}
		
		public Transform transform
		{
			get; protected set;
		}
		
		public Transition PrepareTransition(PanelTransition t, OnTransitionFrameDelegate onFrameDel = null)
		{
			Transition trans;
			GetTransition(t.name, out trans);
			return trans;
		}
						
		public void StartTransition(PanelTransition t)
		{
			Transition trans;
			StartTransition(t.name, out trans);
		}
		
		public void BringInImmediate()
		{
			StartAndCompleteTransition(PanelTransition.IN.name);
		}

		public void DismissImmediate()
		{
			StartAndCompleteTransition(PanelTransition.OUT.name);
		}
		
		private void StartAndCompleteTransition(string name)
		{
			Transition trans;
			if(StartTransition(name, out trans)) {
				trans.CompleteEarly();
			}
		}
		
		private bool StartTransition(string name, out Transition trans)
		{
			if(GetTransition(name, out trans)) {
				trans.StartTransition(Time.time);
				return true;
			}
			else {
				return false;
			}
		}
		
		private bool GetTransition(string name, out Transition t)
		{
			Transition[] tlist;
			GetTransitions(name, out tlist);
			if(tlist != null && tlist.Length > 0) {
				if(tlist.Length == 1) {
					t = tlist[0];
				}
				else {
					var jt = new JoinTransition();
					t = jt;
					for(int i = 0; i < tlist.Length; i++) {
						jt.Add(tlist[i]);
					}
				}
				
				return true;
			}
			else {
				t = null;
				return false;
			}
		}
		
		public bool GetTransitions(string name, out Transition[] tlist)
		{
			if(m_transitionsByName == null) {
				m_transitionsByName = TransitionUtils.FindAndGroupTransitions(this.transform);
			}
			return m_transitionsByName.TryGetValue(name, out tlist);
		}
		
		private Dictionary<string, Transition[]> m_transitionsByName;
		
	}
}
