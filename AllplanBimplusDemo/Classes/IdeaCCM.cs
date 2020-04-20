using IdeaRS.OpenModel;
using IdeaStatiCa.Plugin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BimPlus.Client;
using BimPlus.Client.Integration;
using BimPlus.Sdk.Data.DbCore;
using BimPlus.Sdk.Data.DbCore.Analysis;
using BimPlus.Sdk.Data.DbCore.Steel;
using BimPlus.Sdk.Data.DbCore.Structure;
using BimPlus.Sdk.Utilities.V2;
using IdeaRS.OpenModel.CrossSection;
using IdeaRS.OpenModel.Geometry3D;
using IdeaRS.OpenModel.Model;

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
        private readonly IntegrationBase _integrationBase;
        private List<Guid> selectedIds;
        private List<DtObject> _selectedObjects;

        public IdeaCCM(IntegrationBase integrationBase)
        {
            this._integrationBase = integrationBase;
            _integrationBase.EventHandlerCore.ObjectSelected += EventHandlerCore_ObjectSelected;
            // TODO missing _integrationBase.EventHandlerCore.ObjectSelected += EventHandlerCore_ObjectSelected in destructor
            selectedIds = new List<Guid>();
            _selectedObjects = new List<DtObject>();
        }

        private void EventHandlerCore_ObjectSelected(object sender, BimPlus.Client.BimPlusEventArgs e)
        {
            if (e.Id == Guid.Empty && e.Selected == true)
                return;
            DtObject dtObject = _integrationBase.ApiCore.DtObjects.GetObject(e.Id, ObjectRequestProperties.InternalValues);

            if (dtObject != null)
            {
                if (e.Multiselect == true)
                {
                    if (_selectedObjects.FirstOrDefault(o => o.Id == e.Id) == null)
                        _selectedObjects.Add(dtObject);
                }
                else
                {
                    _selectedObjects.Clear();
                    if (e.Selected == true)
                        _selectedObjects.Add(dtObject);
                }
            }

            if (e.Selected == false)
            {
                DtObject exist = _selectedObjects.FirstOrDefault(o => o.Id == e.Id);
                if (exist != null)
                    _selectedObjects.Remove(exist);
                else
                    _selectedObjects.Clear();
            }
            else if (e.Selected == null && e.Id != Guid.Empty)
            {
                if (dtObject != null)
                {
                    if (_selectedObjects.FirstOrDefault(o => o.Id == e.Id) == null)
                        _selectedObjects.Add(dtObject);
                }
                else
                    _selectedObjects.Clear();
            }

            List<DtObject> newList = new List<DtObject>();
            newList.AddRange(_selectedObjects);
            _selectedObjects = newList;
            //Trace.WriteLine(string.Format("_selectedObjects.Count:{0}", _selectedObjects.Count()));
        }


        protected override string ApplicationName => "BimPlus";

        public override void ActivateInBIM(List<BIMItemId> items)
        {
        }

        protected override ModelBIM ImportActive(CountryCode countryCode, RequestedItemsType requestedType)
        {
            if (_selectedObjects?.Count == 0)
            {
                Debug.Fail("Nothing selected");
                return null;
            }

            var mBim = new ModelBIM
            {
                Project = _integrationBase.CurrentProject.Name,
                // Project = _integrationBase.CurrentProject.Id.ToString()  Maybe Id --> is unique
                RequestedItems = requestedType,
                Model = new OpenModel
                {

                }
            };

            foreach (var obj in _selectedObjects)
            {
                if (obj is StructuralPointConnection point)
                {
                    mBim.Model.Point3D.Add(new Point3D
                    {
                        X = point.X.GetValueOrDefault(),
                        Y = point.Y.GetValueOrDefault(),
                        Z = point.Z.GetValueOrDefault(),
                        Id = point.NodeId.GetValueOrDefault()
                    });
                }
                else if (obj is ElementAssembly ass)
                {
                    mBim.Model.Member1D.Add(new Member1D
                    {
                        Name = ass.Name
                    });
                }
            }

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
