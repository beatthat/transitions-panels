using BeatThat.UI;

namespace BeatThat
{
	public class TransitionsPanel : PanelBase
	{
		protected override Panel _panel
		{
			get {
				if(m_panel == null) {
					m_panel = new PocoTransitionsPanel(this.transform);
				}
				return m_panel;
			}
		}
		
		private Panel m_panel;
	}
}
