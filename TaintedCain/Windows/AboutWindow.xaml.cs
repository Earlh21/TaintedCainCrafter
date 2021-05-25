using System.Reflection;

namespace TaintedCain
{
	public partial class AboutWindow
	{
		public string Version { get; }
	
		public AboutWindow()
		{
			Version = "Version " + Assembly.GetExecutingAssembly().GetName().Version;
			
			InitializeComponent();
		}
	}
}