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
using BimPlus.Sdk.Data.DbCore.Connection;
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
using WM = System.Windows.Media.Media3D;

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
        private List<DtObject> _selectedObjects;

        public static string CrossSectionAttributeId = "32b57db0-f4a1-49e7-ab8b-0d81f0bf8684";
        public static string MaterialAttributeId = "f2d74244-feb2-45e3-b87b-07ef37bb7174";
        public static string RotationAttributeId = "e47e1bcc-7f14-4f91-9222-17b8fb15bcdc";

        public IdeaCCM(IntegrationBase integrationBase)
        {
            this._integrationBase = integrationBase;
            _integrationBase.EventHandlerCore.ObjectSelected += EventHandlerCore_ObjectSelected;
            // TODO missing _integrationBase.EventHandlerCore.ObjectSelected += EventHandlerCore_ObjectSelected in destructor
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

        public static bool IsZero (double value, double tolerance = 1e-9)
        {
            return Math.Abs(value) < tolerance;
        }

        public static bool IsGreater(double leftValue, double rightValue, double tolerance = 1e-10)
        {
            return (leftValue - rightValue) >= tolerance;
        }

        public static void GetAngles(WM.Vector3D dirVect, out double alpha, out double beta)
        {
            const double precision = 1e-6;
            dirVect.Normalize();

            bool isXZero = IsZero(dirVect.X, precision);
            bool isYZero = IsZero(dirVect.Y, precision);
            bool isZZero = IsZero(dirVect.Z, precision);

            if (isYZero && isXZero)
            {
                alpha = 0.0;
                if (dirVect.Z > 0)
                {
                    beta = Math.PI / 2;
                }
                else
                {
                    beta = -Math.PI / 2;
                }
                return;
            }
            else if (isYZero && isZZero)
            {
                alpha = 0.0;
                if (dirVect.X > 0)
                {
                    beta = 0;
                }
                else
                {
                    alpha = Math.PI;
                    beta = 0;
                }
                return;
            }
            else if (isXZero && isZZero)
            {
                beta = 0.0;
                if (dirVect.Y > 0)
                {
                    alpha = Math.PI/2;
                }
                else
                {
                    alpha = -Math.PI / 2;
                }
                return;
            }

            alpha = Math.Atan2(dirVect.Y, dirVect.X);
            if (IsZero(dirVect.Y))
            {
                alpha = IsGreater(dirVect.X,0) ? 0 : Math.PI;
                beta = Math.Atan2(dirVect.Z, Math.Abs(dirVect.X));
            }
            else
            {
                double xy = Math.Sqrt(dirVect.X * dirVect.X + dirVect.Y * dirVect.Y);
                beta = Math.Atan2(dirVect.Z, xy);

                if (IsEqual(alpha,Math.PI, precision) || IsEqual(-alpha,Math.PI, precision))
                {
                    alpha = Math.PI;
                }
            }
        }

        protected override ModelBIM ImportActive(CountryCode countryCode, RequestedItemsType requestedType)
        {
            try
            {

                if (_selectedObjects?.Count == 0)
                {
                    Debug.Fail("Nothing selected");
                    return null;
                }

                var materials = new Dictionary<string, MatSteelEc2>();
                var crossSections = new Dictionary<string, CrossSectionParameter>();
                var pointConnections = _selectedObjects.OfType<StructuralPointConnection>().ToList();
                var curveMembers = _selectedObjects.OfType<StructuralCurveMember>().ToList();
                var assemblies = _selectedObjects.OfType<ElementAssembly>().ToList();
                if (pointConnections.Count == 0)
                {
                    MessageBoxHelper.ShowInformation("please select a PointConnection object to identify the connectionPoint", null);
                    return null;
                }

                // read all nodes (maybe it's esier)
                var nodes = _integrationBase.ApiCore.DtObjects.GetObjects<StructuralPointConnection>(
                    _integrationBase.CurrentProject.Id, false, false, true);

                

                OpenModel openModel = new OpenModel
                {
                    OriginSettings = new OriginSettings() { CrossSectionConversionTable = IdeaRS.OpenModel.CrossSectionConversionTable.NoUsed, CountryCode = countryCode }
                };


                int ccsId = 1;
                int matId = 1;
                int lsId = 1;
                int memberId = 1;
                // ConnectionPoint
                ConnectionPoint connectionPoint = null;
                foreach (var point in pointConnections)
                {
                    // ad main node to openModel
                    connectionPoint = new ConnectionPoint();
                    if (point.NodeId.HasValue)
                        ConnectionPointId = point.NodeId.Value;
                    Point3D node = AddNodeToOpenModel(point, openModel, ConnectionPointId);
                    connectionPoint.Node = new ReferenceElement(node);

                    connectionPoint.Id = ConnectionPointId;
                    connectionPoint.Name = string.Format("Conn-{0}", ConnectionPointId);
                    connectionPoint.ProjectFileName = Path.Combine(".\\Connections", connectionPoint.Name + ".ideaCon");
                }


                // try to add Member1D objects if they are not selected by user
                foreach (var assembly in assemblies)
                {
                    if (assembly.Connections == null) continue;
                    foreach (var c in assembly.Connections)
                    {
                        var ce = c as RelConnectsElements;
                        if (ce == null || curveMembers.Find(x => x.Id == ce.RelatedElement.Value) != null) continue;
                        var cm = _integrationBase.ApiCore.DtObjects.GetObjectInternal(ce.RelatedElement.Value) as StructuralCurveMember;
                        if (cm != null)
                        {
                            foreach (var cm1 in pointConnections[0].ConnectsStructuralMembers)
                            {
                                if(cm.Id.ToString() == cm1.RelatingStructuralMember.GetValueOrDefault().ToString())
                                {
                                    curveMembers.Add(cm);
                                }
                            }
                        }
                        }
                    }
                    // create Element1D from StructuralCurveMember
                    foreach (var element in curveMembers)
                {
                    var matName = element.GetStringProperty(TableNames.contentAttributes, MaterialAttributeId) ?? "S355";
                    if (!materials.ContainsKey(matName))
                    {
                        MatSteelEc2 material = new MatSteelEc2();

                        // set properties
                        material.Id = matId++;
                        material.Name = matName;
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
                        materials.Add(matName, material);
                    }

                    var crossSection = element.GetStringProperty(TableNames.contentAttributes, CrossSectionAttributeId) ?? "HE200B";
                    if (!crossSections.ContainsKey(crossSection))
                    {
                        crossSection = crossSection.Replace("HE200B", "HEB200");
                        crossSection = crossSection.Replace("HE240B", "HEB240");
                        CrossSectionParameter css = new CrossSectionParameter
                        {
                            Id = ccsId++,
                            Name = crossSection,
                            CrossSectionRotation = 0,
                            CrossSectionType = CrossSectionType.RolledI,
                            Material = new ReferenceElement(materials[matName])
                        };
                        css.Parameters.Add(new ParameterString() { Name = "UniqueName", Value = crossSection });

                        // add cross sections to the model
                        openModel.AddObject(css);
                        crossSections.Add(crossSection, css);
                    }

                    if (element?.ConnectedBy.Count != 2)
                        continue;

                    var node = nodes.Find(x => x.Id == element?.ConnectedBy[0].RelatedStructuralConnection.Value);
                    if (node == null) continue;
                    Point3D ptA = AddNodeToOpenModel(node, openModel, node.NodeId.GetValueOrDefault());

                    node = nodes.Find(x => x.Id == element?.ConnectedBy[1].RelatedStructuralConnection.Value);
                    Point3D ptB = AddNodeToOpenModel(node, openModel, node.NodeId.GetValueOrDefault());

                    var member1d = new Member1D
                    {
                        Id = memberId++,
                        Name = element.Name
                    };

                    IdeaRS.OpenModel.Geometry3D.PolyLine3D polyLine3D = new IdeaRS.OpenModel.Geometry3D.PolyLine3D
                    {
                        Id = member1d.Id
                    };

                    var start = new WM.Point3D(ptA.X, ptA.Y, ptA.Z);
                    var end = new WM.Point3D(ptB.X, ptB.Y, ptB.Z);
                    var dirVectort = end - start;

                    GetAngles(dirVectort, out double alpha, out double beta);
                    beta *= -1;

                    CI.Geometry3D.Matrix44 lcsSegmentMatrix = new CI.Geometry3D.Matrix44();

                    if (!IsZero(beta))
                    {
                        // gamma pitch
                        lcsSegmentMatrix.Rotate(beta, new CI.Geometry3D.Vector3D(0, 1, 0));
                    }

                    if (!IsZero(alpha))
                    {
                        // beta direction
                        lcsSegmentMatrix.Rotate(alpha, new CI.Geometry3D.Vector3D(0, 0, 1));
                    }

                    IdeaRS.OpenModel.Geometry3D.LineSegment3D ls = new IdeaRS.OpenModel.Geometry3D.LineSegment3D
                    {
                        Id = member1d.Id,
                        StartPoint = new ReferenceElement(ptA),
                        EndPoint = new ReferenceElement(ptB),
                        LocalCoordinateSystem = new IdeaRS.OpenModel.Geometry3D.CoordSystemByVector()
                        {
                            VecX = new IdeaRS.OpenModel.Geometry3D.Vector3D()
                            {
                                X = lcsSegmentMatrix.AxisX.DirectionX,
                                Y = lcsSegmentMatrix.AxisX.DirectionY,
                                Z = lcsSegmentMatrix.AxisX.DirectionZ,
                            }
            ,
                            VecY = new IdeaRS.OpenModel.Geometry3D.Vector3D()
                            {
                                X = lcsSegmentMatrix.AxisY.DirectionX,
                                Y = lcsSegmentMatrix.AxisY.DirectionY,
                                Z = lcsSegmentMatrix.AxisY.DirectionZ,
                            }
            ,
                            VecZ = new IdeaRS.OpenModel.Geometry3D.Vector3D()
                            {
                                X = lcsSegmentMatrix.AxisZ.DirectionX,
                                Y = lcsSegmentMatrix.AxisZ.DirectionY,
                                Z = lcsSegmentMatrix.AxisZ.DirectionZ,
                            }
                        },
                    };
                    polyLine3D.Segments.Add(new ReferenceElement(ls));

                    openModel.PolyLine3D.Add(polyLine3D);
                    openModel.LineSegment3D.Add(ls);
                   
                    Element1D element1d = new Element1D
                    {
                        Id = member1d.Id,
                        Name = element.Id.ToString(), //element.Name, Its esier for mapping Element1D with bimplus Structuralcurvemember
                        RotationRx = element.GetDoubleProperty(TableNames.contentAttributes, RotationAttributeId) ?? 0,
                        CrossSectionBegin = new ReferenceElement(crossSections[crossSection]),
                        CrossSectionEnd = new ReferenceElement(crossSections[crossSection]),
                        Segment = new ReferenceElement(ls)
                    };
                    openModel.Element1D.Add(element1d);

                    member1d.Elements1D.Add(new ReferenceElement(element1d));

                    openModel.Member1D.Add(member1d);

                    if (connectionPoint != null)
                    {
                        ConnectedMember conMb = new ConnectedMember
                        {
                            Id = member1d.Id,
                            MemberId = new ReferenceElement(member1d),
                            IsContinuous = false,
                        };

                        connectionPoint.ConnectedMembers.Add(conMb);
                    }

                    BeamData beamData = new BeamData
                    {
                        Id = member1d.Id,
                        OriginalModelId = member1d.Id.ToString(),
                        IsAdded = false,
                        MirrorY = false,
                        RefLineInCenterOfGravity = true,
                    };

                    if (openModel.Connections.Count == 0)
                    {
                        openModel.Connections.Add(new ConnectionData());
                    }
                    (openModel.Connections[0].Beams ?? (openModel.Connections[0].Beams = new List<BeamData>())).Add(beamData);
                }

                // create Member1D from assemblies
                /* foreach (var assembly in assemblies)
                 {
                     var member1d = new Member1D
                     {
                         Id = assembly.OrderNumber.GetValueOrDefault(),
                         Name = assembly.Name
                     };
                     foreach (var c in assembly.Connections)
                     {
                         var ce = c as RelConnectsElements;
                         var element = openModel.Element1D.Find(x => x.Name == ce.RelatedElement.ToString());
                         if (element == null) continue;
                         member1d.Elements1D.Add(new ReferenceElement(element));
                         openModel.Member1D.Add(member1d);
                     }
                 }*/

                /*if (connectionPoint != null)
                 {
                     var p0t = pointConnections.Find(x => x.NodeId.GetValueOrDefault() == ConnectionPointId);
                     if (p0t != null && p0t.ConnectsStructuralMembers != null)
                     {
                         foreach (var cm in p0t.ConnectsStructuralMembers)
                         {
                             var member = openModel.Element1D.Find(x => x.Name == cm.RelatingStructuralMember.GetValueOrDefault().ToString());
                             if (member == null)
                                 continue;
                             ConnectedMember conMb = new ConnectedMember
                             {
                                 Id = member.Id,
                                 MemberId = new ReferenceElement(member),
                                 IsContinuous = false,
                             };
                             connectionPoint.ConnectedMembers.Add(conMb);

                             // BEAM DATA - definition
                             BeamData bData = new BeamData
                             {
                                 Id = conMb.Id,
                                 OriginalModelId = "???", // ass.Id.ToString(), is it important?
                                 IsAdded = false,
                                 MirrorY = false,
                                 RefLineInCenterOfGravity = false,
                             };
                             if (openModel.Connections.Count == 0)
                             {
                                 openModel.Connections.Add(new ConnectionData { Beams = new List<BeamData>() });
                             }
                             openModel.Connections[0].Beams.Add(bData);
                         }
                     }

                 }*/
                openModel.AddObject(connectionPoint); // important !!!

                OpenModelResult openModelResult =
                new OpenModelResult()
                {
                    ResultOnMembers = new System.Collections.Generic.List<ResultOnMembers>() { new ResultOnMembers() }
                };
                OpenMessages openMessages = new OpenMessages();

                return new ModelBIM()
                {
                    Items = new List<BIMItemId>() { new BIMItemId() { Id = connectionPoint.Id, Type = BIMItemType.Node } },
                    Model = openModel,
                    Results = openModelResult,
                    Messages = openMessages,
                    Project = AllplanBimplusDemo.Properties.Settings.Default.IdeaDefaultWorkingDir,
                };
            }
            catch (Exception e)
            {
                MessageBoxHelper.ShowInformation(e.Message, null);
                return null;

            }
        }

        protected override List<ModelBIM> ImportSelection(CountryCode countryCode, List<BIMItemsGroup> items)
        {
            Debug.Fail("TODO");
            return null;
        }
    }
}
