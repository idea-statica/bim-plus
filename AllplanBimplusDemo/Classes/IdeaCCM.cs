using IdeaRS.OpenModel;
using IdeaStatiCa.Plugin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllplanBimplusDemo.Classes
{
	public class IdeaHistoryLog : IHistoryLog
	{
		public void Add(string action)
		{
		}
	}

	public class IdeaCCM : ApplicationBIM
	{
		protected override string ApplicationName => throw new NotImplementedException();

		public override void ActivateInBIM(List<BIMItemId> items)
		{
		}

		protected override ModelBIM ImportActive(CountryCode countryCode, RequestedItemsType requestedType)
		{
			Debug.Fail("TODO");
			return null;
		}

		protected override List<ModelBIM> ImportSelection(CountryCode countryCode, List<BIMItemsGroup> items)
		{
			Debug.Fail("TODO");
			return null;
		}
	}
}
