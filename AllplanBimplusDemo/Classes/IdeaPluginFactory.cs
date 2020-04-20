using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IdeaStatiCa.Plugin;
using System.IO;

namespace AllplanBimplusDemo.Classes
{
		public interface IHistoryLog
		{
			void Add(string action);
		}

		public class PluginFactory : IBIMPluginFactory
		{
			private IHistoryLog log;

			public PluginFactory(IHistoryLog log)
			{
				this.log = log;
			}

			public string FeaAppName => "BIM Plus";

			public IApplicationBIM Create()
			{
				return new IdeaCCM();
			}

			public string IdeaStaticaAppPath
			{
				get
				{
					return "path to idea install";
				}
			}
		}
	}
