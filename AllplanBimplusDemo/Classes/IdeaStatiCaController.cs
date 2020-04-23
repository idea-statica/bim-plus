using BimPlus.Client.Integration;
using IdeaRS.OpenModel.Connection;
using IdeaStatiCa.Plugin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllplanBimplusDemo.Classes
{
	public class IdeaStatiCaController
	{
		IBIMPluginHosting feaAppHosting;
    static string AppTempDir { get; set; }
    IntegrationBase MyIntegrationBase { get; set; }


    public IBIMPluginHosting FeaAppHosting { get => feaAppHosting; set => feaAppHosting = value; }

    static IdeaStatiCaController()
    {
      AppTempDir = AllplanBimplusDemo.Properties.Settings.Default.IdeaStatiCaCCM;
    }

    internal IdeaStatiCaController(IntegrationBase integrationBase)
    {
      MyIntegrationBase = integrationBase;
    }

    public void RunIdeaStaticaCCM(object param)
    {
      if(FeaAppHosting != null)
      {
        return;
      }

      var factory = new PluginFactory(new IdeaHistoryLog(), MyIntegrationBase);
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

    public ConnectionData GetConnectionModel(int connectionId)
    {
      if (FeaAppHosting == null)
      {
        return null;
      }

      var bimAppliction = (ApplicationBIM)FeaAppHosting.Service;
      if(bimAppliction == null)
      {
        Debug.Fail("Can not cast to ApplicationBIM");
        return null;
      }

      ConnectionData connectionData = null;
      int myProcessId = bimAppliction.Id;

      using (IdeaStatiCaAppClient ideaStatiCaApp = new IdeaStatiCaAppClient(myProcessId.ToString()))
      {
        ideaStatiCaApp.Open();

        connectionData = ideaStatiCaApp.GetConnectionModel(1);
      }
        
      return connectionData;
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
