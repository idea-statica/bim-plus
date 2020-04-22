using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IdeaStatiCa.Plugin;
using System.IO;
using BimPlus.Client.Integration;

namespace AllplanBimplusDemo.Classes
{
	public interface IHistoryLog
	{
		void Add(string action);
	}

	public class PluginFactory : IBIMPluginFactory
	{
		private readonly IntegrationBase _integrationBase;
		private IHistoryLog log;

		public PluginFactory(IHistoryLog log, IntegrationBase intBase)
		{
			this.log = log;
			this._integrationBase = intBase;
		}

		public string FeaAppName => "BimPlus";

		public IApplicationBIM Create()
		{
			return new IdeaCCM(_integrationBase);
		}

		public string IdeaStaticaAppPath
		{
			get
			{
				return AllplanBimplusDemo.Properties.Settings.Default.IdeaStatiCaCCM;
			}
		}
	}
}
