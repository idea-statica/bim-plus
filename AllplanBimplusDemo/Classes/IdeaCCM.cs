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
using IdeaRS.OpenModel.Connection;
using System.IO;
using IdeaRS.OpenModel.Material;
using IdeaRS.OpenModel.Result;
using IdeaRS.OpenModel.Message;

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

        public override bool IsCAD()
        {
            return true;
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
        
        protected void AddBeam(ElementAssembly beam, OpenModel openModel)
        {

        }

        public int ConnectionPointId { get; set; } = -1;
        private const double limits = 0.001;

        // ad node to openModel
        private Point3D AddNodeToOpenModel(StructuralPointConnection conPoint, OpenModel openStructModel, int id = 0)
        {
            var pt = openStructModel.Point3D.Find(c => IsEqual(c.X, conPoint.X.GetValueOrDefault(), limits) && IsEqual(c.Y, conPoint.Y.GetValueOrDefault(), limits) && IsEqual(c.Z, conPoint.Z.GetValueOrDefault(), limits));
            if (pt != null)
            {
                return pt;
            }
            else
            {
                IdeaRS.OpenModel.Geometry3D.Point3D point = new IdeaRS.OpenModel.Geometry3D.Point3D() { X = conPoint.X.GetValueOrDefault(), Y = conPoint.Y.GetValueOrDefault(), Z = conPoint.Z.GetValueOrDefault() };
                point.Id = (id > 0 ? id : (openStructModel.GetMaxId(point) + 1));
                point.Name = point.Id.ToString();
                openStructModel.Point3D.Add(point);
                return point;
            }
        }

        public static bool IsEqual(double leftValue, double rightValue, double tolerance = 1e-10)
        {
            if ((double.IsPositiveInfinity(leftValue) && double.IsPositiveInfinity(rightValue))
                || (double.IsNegativeInfinity(leftValue) && double.IsNegativeInfinity(rightValue)))
            {
                return true;
            }

            return Math.Abs(leftValue - rightValue) <= tolerance;
        }



        protected override ModelBIM ImportActive(CountryCode countryCode, RequestedItemsType requestedType)
        {
            if (_selectedObjects?.Count == 0)
            {
                Debug.Fail("Nothing selected");
                return null;
            }

            OpenModel openModel = new OpenModel
            {
                OriginSettings = new OriginSettings() { CrossSectionConversionTable = IdeaRS.OpenModel.CrossSectionConversionTable.NoUsed, CountryCode = countryCode }
            };

            



            var mBim = new ModelBIM
            {
                Project = _integrationBase.CurrentProject.Name,
                // Project = _integrationBase.CurrentProject.Id.ToString()  Maybe Id --> is unique
                RequestedItems = requestedType,
                Model = new OpenModel
                {

                }
            };

            int ccsId = 1;
            int matId = 1;
            int memberId = 1;
            ConnectionPoint connectionPoint = null;
            foreach (var obj in _selectedObjects)
            {
                if (obj is StructuralPointConnection point)
                {
                    // ad main node to openModel
                    connectionPoint = new ConnectionPoint();
                    Point3D node = AddNodeToOpenModel(point, openModel, ConnectionPointId);
                    connectionPoint.Node = new ReferenceElement(node);

                    connectionPoint.Id = ConnectionPointId;
                    connectionPoint.Name = string.Format("Conn-{0}", ConnectionPointId);
                    connectionPoint.ProjectFileName = Path.Combine(".\\Connections", connectionPoint.Name + ".ideaCon");
                }
            }

            foreach (var obj in _selectedObjects)
            {
                    
               if (obj is ElementAssembly ass)
               {
                    MatSteelEc2 material = new MatSteelEc2();

                    // set properties
                    material.Id = matId++;
                    material.Name = "S355";
                    material.E = 210000000000;
                    material.G = material.E / (2 * (1 + 0.3));
                    material.Poisson = 0.3;
                    material.UnitMass = 7850;
                    material.SpecificHeat = 0.6;
                    material.ThermalExpansion = 0.000012;
                    material.ThermalConductivity = 45;
                    material.IsDefaultMaterial = false;
                    material.OrderInCode = 0;
                    material.StateOfThermalExpansion = ThermalExpansionState.Code;
                    material.StateOfThermalConductivity = ThermalConductivityState.Code;
                    material.StateOfThermalSpecificHeat = ThermalSpecificHeatState.Code;
                    material.StateOfThermalStressStrain = ThermalStressStrainState.Code;
                    material.StateOfThermalStrain = ThermalStrainState.Code;
                    material.fy = 355000000;
                    material.fu = 510000000;
                    material.fy40 = 335000000;
                    material.fu40 = 470000000;
                    material.DiagramType = SteelDiagramType.Bilinear;

                    // add material to the model
                    openModel.AddObject(material);

                    CrossSectionParameter css = new CrossSectionParameter();
                    css.Id = ccsId++;
                    css.Name = "HE200B";
                    css.CrossSectionRotation = 0;
                    css.CrossSectionType = CrossSectionType.RolledI;

                    css.Parameters.Add(new ParameterString() { Name = "UniqueName", Value = "HE200B" });
                    css.Material = new ReferenceElement(material);

                    // add cross sections to the model
                    openModel.AddObject(css);


                    Member1D mb = new Member1D
                    {
                        Id = memberId++,
                    };

                    string nameBar = "B" + mb.Id.ToString();
                    // LINE SEGMENT - LCS geometry
                    IdeaRS.OpenModel.Geometry3D.PolyLine3D polyLine3D = new IdeaRS.OpenModel.Geometry3D.PolyLine3D
                    {
                        Id = mb.Id
                    };
                    IdeaRS.OpenModel.Geometry3D.LineSegment3D ls = new IdeaRS.OpenModel.Geometry3D.LineSegment3D
                    {
                        Id = mb.Id
                    };

                    // ELEMENT 1D - definition
                    Element1D el = new Element1D
                    {
                        Id = mb.Id,
                        Name = "E" + nameBar,
                        Segment = new ReferenceElement(ls)
                    };

                    openModel.PolyLine3D.Add(polyLine3D);
                    openModel.LineSegment3D.Add(ls);

                    // NEED TODO
                    /* Point3D ptA = AddNodeToOpenModel(centLineStart, openModel);
                     Point3D ptB = AddNodeToOpenModel(centLineEnd, openModel);

                     ls.StartPoint = new ReferenceElement(ptA);
                     ls.EndPoint = new ReferenceElement(ptB);

                     ls.LocalCoordinateSystem = new IdeaRS.OpenModel.Geometry3D.CoordSystemByVector()
                     {
                         VecX = new IdeaRS.OpenModel.Geometry3D.Vector3D()
                         {
                             X = ,
                             Y = ,
                             Z = 
                         }
             ,
                         VecY = new IdeaRS.OpenModel.Geometry3D.Vector3D()
                         {
                             X = ,
                             Y = ,
                             Z = 
                         }
             ,
                         VecZ = new IdeaRS.OpenModel.Geometry3D.Vector3D()
                         {
                             X = ,
                             Y = ,
                             Z = 
                         }
                     };*/

                    polyLine3D.Segments.Add(new ReferenceElement(ls));

                    //el.RotationRx = ;

                    el.CrossSectionBegin = new ReferenceElement(css);
                    el.CrossSectionEnd = new ReferenceElement(css);

                    openModel.Element1D.Add(el);
                    mb.Elements1D.Add(new ReferenceElement(el));
                    openModel.Member1D.Add(mb);

                    if (connectionPoint != null)
                    {
                        ConnectedMember conMb = new ConnectedMember
                        {
                            Id = mb.Id,
                            MemberId = new ReferenceElement(mb),
                            IsContinuous = false,
                        };

                        connectionPoint.ConnectedMembers.Add(conMb);
                    }

                    // BEAM DATA - definition
                    BeamData beamData = new BeamData
                    {
                        Id = mb.Id,
                        OriginalModelId = ass.Id.ToString(),
                        IsAdded = false,
                        MirrorY = false,
                        RefLineInCenterOfGravity = false,
                    };

                    if (openModel.Connections.Count == 0)
                    {
                        openModel.Connections.Add(new ConnectionData());
                    }

                    (openModel.Connections[0].Beams ?? (openModel.Connections[0].Beams = new List<BeamData>())).Add(beamData);
                }
            }

            openModel.AddObject(connectionPoint); // important !!!

            OpenModelResult openModelResult =
            new OpenModelResult()
            {
                ResultOnMembers = new System.Collections.Generic.List<ResultOnMembers>() { new ResultOnMembers() }
            };
            OpenMessages openMessages = new OpenMessages();

            return new ModelBIM()
            {
                Items = new List<BIMItemId>() { new BIMItemId() { Id = 1, Type = BIMItemType.Node } },
                Model = openModel,
                Results = openModelResult,
                Messages = openMessages,
                Project = $"C:\\temp\\BIMpLus",
            };
        }

        protected override List<ModelBIM> ImportSelection(CountryCode countryCode, List<BIMItemsGroup> items)
        {
            Debug.Fail("TODO");
            return null;
        }
    }
}
