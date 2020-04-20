using IdeaStatiCa.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllplanBimplusDemo.Classes
{
	public class IdeaStatiCaController
	{
		IBIMPluginHosting feaAppHosting;
    static string AppTempDir { get; set; }

		public IBIMPluginHosting FeaAppHosting { get => feaAppHosting; set => feaAppHosting = value; }

    static IdeaStatiCaController()
    {
      AppTempDir = AllplanBimplusDemo.Properties.Settings.Default.IdeaStatiCaCCM;
    }

    public void RunIdeaStaticaCCM(object param)
    {
      if(FeaAppHosting != null)
      {
        return;
      }

      var factory = new PluginFactory(new IdeaHistoryLog());
      FeaAppHosting = new BIMPluginHosting(factory);
      FeaAppHosting.AppStatusChanged += new ISEventHandler(IdeaStaticAppStatusChanged);
      var id = System.Diagnostics.Process.GetCurrentProcess().Id.ToString();

      //ProjectDir = Path.Combine(WorkingDirectory, ProjectName);
      //if (!Directory.Exists(ProjectDir))
      //{
      //  Directory.CreateDirectory(ProjectDir);
      //}

      //var ideaStatiCaProjectDir = Path.Combine(ProjectDir, "IdeaStatiCa-" + ProjectName);
      //if (!Directory.Exists(ideaStatiCaProjectDir))
      //{
      //  Directory.CreateDirectory(ideaStatiCaProjectDir);
      //}

      //Add(string.Format("Starting FEAPluginHosting clientTd = {0}", id));

      string ideaStatiCaProjectDir = AllplanBimplusDemo.Properties.Settings.Default.IdeaDefaultWorkingDir;

      FeaAppHosting.RunAsync(id, ideaStatiCaProjectDir);


    }

		private void IdeaStaticAppStatusChanged(object sender, ISEventArgs e)
		{
			if (e.Status == AppStatus.Finished)
			{
        FeaAppHosting.AppStatusChanged -= new ISEventHandler(IdeaStaticAppStatusChanged);
        FeaAppHosting = null;
			}
		}
	}
}
