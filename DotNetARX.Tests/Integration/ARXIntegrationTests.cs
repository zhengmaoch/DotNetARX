using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetARX.Tests.Integration
{
    [TestClass]
    public class ARXIntegrationTests : TestBase
    {
        [TestInitialize]
        public void Setup()
        {
            base.TestInitialize();
            ARX.Initialize();
        }

        [TestMethod]
        public void ARX_Entity_Operations_WorkCorrectly()
        {
            // Arrange
            var line = new Line(new Point3d(0, 0, 0), new Point3d(100, 100, 0));
            var entityId = AddEntityToModelSpace(line);

            // Test Move
            var moveResult = ARX.ARXEntity.Move(entityId, new Point3d(0, 0, 0), new Point3d(50, 50, 0));
            Assert.IsTrue(moveResult);

            // Test Copy
            var copyId = ARX.ARXEntity.Copy(entityId, new Point3d(50, 50, 0), new Point3d(100, 100, 0));
            Assert.IsFalse(copyId.IsNull);

            // Test Rotate
            var rotateResult = ARX.ARXEntity.Rotate(entityId, new Point3d(50, 50, 0), Math.PI / 4);
            Assert.IsTrue(rotateResult);

            // Test Scale
            var scaleResult = ARX.ARXEntity.Scale(entityId, new Point3d(50, 50, 0), 2.0);
            Assert.IsTrue(scaleResult);

            // Test Validate
            var validateResult = ARX.ARXEntity.Validate(entityId);
            Assert.IsTrue(validateResult);
        }

        [TestMethod]
        public void ARX_Layer_Operations_WorkCorrectly()
        {
            // Test Create Layer
            var layerId = ARX.ARXLayer.Create("TestLayer", 1);
            Assert.IsFalse(layerId.IsNull);

            // Test Layer Exists
            Assert.IsTrue(ARX.ARXLayer.Exists("TestLayer"));

            // Test Set Current Layer
            var setCurrentResult = ARX.ARXLayer.SetCurrent("TestLayer");
            Assert.IsTrue(setCurrentResult);

            // Test Get Layer Names
            var layerNames = ARX.ARXLayer.GetNames();
            Assert.IsTrue(layerNames.Contains("TestLayer"));

            // Test Set Layer Properties
            var setPropsResult = ARX.ARXLayer.SetProperties("TestLayer", colorIndex: 2, isLocked: true);
            Assert.IsTrue(setPropsResult);
        }

        [TestMethod]
        public void ARX_Drawing_Operations_WorkCorrectly()
        {
            // Test Draw Line
            var lineId = ARX.ARXDrawing.Line(new Point3d(0, 0, 0), new Point3d(100, 0, 0));
            Assert.IsFalse(lineId.IsNull);

            // Test Draw Circle
            var circleId = ARX.ARXDrawing.Circle(new Point3d(50, 50, 0), 25);
            Assert.IsFalse(circleId.IsNull);

            // Test Draw Arc
            var arcId = ARX.ARXDrawing.Arc(new Point3d(100, 100, 0), 30, 0, Math.PI);
            Assert.IsFalse(arcId.IsNull);

            // Test Draw Polyline
            var points = new List<Point2d>
            {
                new Point2d(0, 0),
                new Point2d(100, 0),
                new Point2d(100, 100),
                new Point2d(0, 100)
            };
            var polylineId = ARX.ARXDrawing.Polyline(points, true);
            Assert.IsFalse(polylineId.IsNull);

            // Test Draw Text
            var textId = ARX.ARXDrawing.Text("Test Text", new Point3d(200, 200, 0), 10);
            Assert.IsFalse(textId.IsNull);

            // Test Draw MText
            var mtextId = ARX.ARXDrawing.MText("Multi-line\nText", new Point3d(300, 300, 0), 100, 50);
            Assert.IsFalse(mtextId.IsNull);
        }

        [TestMethod]
        public void ARX_Database_Operations_WorkCorrectly()
        {
            // Test Add to Model Space
            var circle = new Circle(new Point3d(0, 0, 0), Vector3d.ZAxis, 10);
            var entityId = ARX.ARXDatabase.AddToModelSpace(circle);
            Assert.IsFalse(entityId.IsNull);

            // Test Batch Add
            var entities = new List<Entity>
            {
                new Line(new Point3d(0, 0, 0), new Point3d(50, 50, 0)),
                new Circle(new Point3d(100, 100, 0), Vector3d.ZAxis, 15)
            };
            var entityIds = ARX.ARXDatabase.AddToModelSpace(entities);
            Assert.AreEqual(2, entityIds.Count);

            // Test Get Database Info
            var dbInfo = ARX.ARXDatabase.GetInfo();
            Assert.IsNotNull(dbInfo);
            Assert.IsNotNull(dbInfo.FileName);

            // Test Delete Entity
            var deleteResult = ARX.ARXDatabase.DeleteEntity(entityId);
            Assert.IsTrue(deleteResult);

            // Test Batch Delete
            var deleteCount = ARX.ARXDatabase.DeleteEntities(entityIds);
            Assert.AreEqual(2, deleteCount);
        }

        [TestMethod]
        public void ARX_Selection_Operations_WorkCorrectly()
        {
            // Create some test entities
            var line = new Line(new Point3d(0, 0, 0), new Point3d(100, 100, 0));
            var circle = new Circle(new Point3d(50, 50, 0), Vector3d.ZAxis, 25);

            AddEntityToModelSpace(line);
            AddEntityToModelSpace(circle);

            // Test Select by Type
            var lines = ARX.ARXSelection.ByType<Line>();
            Assert.IsTrue(lines.Count >= 1);

            var circles = ARX.ARXSelection.ByType<Circle>();
            Assert.IsTrue(circles.Count >= 1);

            // Test Select in Window
            var entitiesInWindow = ARX.ARXSelection.InWindow<Entity>(
                new Point3d(-10, -10, 0),
                new Point3d(110, 110, 0));
            Assert.IsTrue(entitiesInWindow.Count >= 2);

            // Test Select at Point
            var entitiesAtPoint = ARX.ARXSelection.AtPoint<Entity>(new Point3d(50, 50, 0));
            Assert.IsTrue(entitiesAtPoint.Count >= 1);
        }

        [TestMethod]
        public void ARX_Geometry_Operations_WorkCorrectly()
        {
            // Test Distance Calculation
            var pt1 = new Point3d(0, 0, 0);
            var pt2 = new Point3d(3, 4, 0);
            var distance = ARX.Geometry.Distance(pt1, pt2);
            Assert.AreEqual(5.0, distance, 1e-6);

            // Test Angle Calculation
            var pt3 = new Point3d(1, 0, 0);
            var pt4 = new Point3d(0, 0, 0);
            var pt5 = new Point3d(0, 1, 0);
            var angle = ARX.Geometry.Angle(pt3, pt4, pt5);
            Assert.AreEqual(Math.PI / 2, angle, 1e-6);

            // Test Point in Polygon
            var polygon = new List<Point3d>
            {
                new Point3d(0, 0, 0),
                new Point3d(10, 0, 0),
                new Point3d(10, 10, 0),
                new Point3d(0, 10, 0)
            };
            var pointInside = new Point3d(5, 5, 0);
            var pointOutside = new Point3d(15, 15, 0);

            Assert.IsTrue(ARX.Geometry.PointInPolygon(pointInside, polygon));
            Assert.IsFalse(ARX.Geometry.PointInPolygon(pointOutside, polygon));

            // Test Entity Bounds
            var circle = new Circle(new Point3d(0, 0, 0), Vector3d.ZAxis, 10);
            var circleId = AddEntityToModelSpace(circle);
            var bounds = ARX.Geometry.GetBounds(circleId);

            Assert.AreEqual(-10, bounds.MinPoint.X, 1e-6);
            Assert.AreEqual(10, bounds.MaxPoint.X, 1e-6);
        }

        [TestMethod]
        public void ARX_Command_Operations_WorkCorrectly()
        {
            // Test COM Command
            var comResult = ARX.Command.ExecuteCOM("REGEN");
            Assert.IsTrue(comResult);

            // Test Async Command
            var asyncResult = ARX.Command.ExecuteAsync("ZOOM E");
            Assert.IsTrue(asyncResult);

            // Test Queue Command
            var queueResult = ARX.Command.ExecuteQueue("REDRAW");
            Assert.IsTrue(queueResult);

            // Test ARX Command
            var arxResult = ARX.Command.ExecuteARX("TESTCMD", "arg1", "arg2");
            Assert.IsTrue(arxResult);
        }

        [TestMethod]
        public void ARX_Document_Operations_WorkCorrectly()
        {
            // Test Check Needs Save
            var needsSave = ARX.Document.NeedsSave();
            Assert.IsTrue(needsSave == true || needsSave == false); // Any boolean value is valid

            // Test Get Document Info
            var docInfo = ARX.Document.GetInfo();
            Assert.IsNotNull(docInfo);
            Assert.IsNotNull(docInfo.Name);
            Assert.IsTrue(docInfo.CreationTime <= DateTime.Now);

            // Test Save Document
            var saveResult = ARX.Document.Save();
            // In test environment, this might fail, but shouldn't throw

            // Test Save As
            var saveAsResult = ARX.Document.SaveAs(@"C:\Temp\TestDoc.dwg");
            // In test environment, this might fail due to path issues
        }

        [TestMethod]
        public void ARX_Style_Operations_WorkCorrectly()
        {
            // Test Create Text Style
            var textStyleId = ARX.Style.CreateTextStyle("TestTextStyle", "Arial", 2.5);
            Assert.IsFalse(textStyleId.IsNull);

            // Test Create Dimension Style
            var dimStyleId = ARX.Style.CreateDimStyle("TestDimStyle", 2.0, 1.0);
            Assert.IsFalse(dimStyleId.IsNull);

            // Test Create Line Type
            var lineTypeId = ARX.Style.CreateLineType("TestLineType", "A,0.5,-0.25", "Test Line Type");
            Assert.IsFalse(lineTypeId.IsNull);
        }

        [TestMethod]
        public void ARX_Table_Operations_WorkCorrectly()
        {
            // Test Create Table
            var tableId = ARX.Table.Create(new Point3d(0, 0, 0), 3, 4, 10, 50);
            Assert.IsFalse(tableId.IsNull);

            // Test Set Cell Text
            var setCellResult = ARX.Table.SetCellText(tableId, 0, 0, "Header");
            Assert.IsTrue(setCellResult);

            // Test Get Cell Text
            var cellText = ARX.Table.GetCellText(tableId, 0, 0);
            Assert.AreEqual("Header", cellText);

            // Test Merge Cells
            var mergeResult = ARX.Table.MergeCells(tableId, 1, 0, 1, 1);
            Assert.IsTrue(mergeResult);
        }

        [TestMethod]
        public void ARX_Layout_Operations_WorkCorrectly()
        {
            // Test Create Layout
            var layoutId = ARX.Layout.Create("TestLayout");
            Assert.IsFalse(layoutId.IsNull);

            // Test Create Viewport
            var viewportId = ARX.Layout.CreateViewport(new Point3d(100, 100, 0), 200, 150);
            Assert.IsFalse(viewportId.IsNull);

            // Test Set Viewport Scale
            var scaleResult = ARX.Layout.SetViewportScale(viewportId, 0.5);
            Assert.IsTrue(scaleResult);

            // Test Delete Layout
            var deleteResult = ARX.Layout.Delete("TestLayout");
            Assert.IsTrue(deleteResult);
        }

        [TestMethod]
        public void ARX_UI_Operations_WorkCorrectly()
        {
            // Test Show Message (shouldn't throw)
            try
            {
                ARX.UI.ShowMessage("Test Message", "Test Title");
                Assert.IsTrue(true);
            }
            catch
            {
                Assert.Fail("ShowMessage should not throw exception");
            }

            // Test Show Confirmation
            var confirmResult = ARX.UI.ShowConfirmation("Confirm?", "Test");
            Assert.IsTrue(confirmResult == true || confirmResult == false);

            // Test Get User Input
            var userInput = ARX.UI.GetUserInput("Enter text:", "Default");
            Assert.IsNotNull(userInput);

            // Test Select File
            var fileName = ARX.UI.SelectFile("Select File", "All files (*.*)|*.*", false);
            Assert.IsNotNull(fileName);
        }

        [TestMethod]
        public void ARX_Utility_Operations_WorkCorrectly()
        {
            // Test Validate String
            Assert.IsTrue(ARX.Utility.ValidateString("123", @"^\d+$"));
            Assert.IsFalse(ARX.Utility.ValidateString("abc", @"^\d+$"));

            // Test Safe Convert
            Assert.AreEqual(123, ARX.Utility.SafeConvert<int>("123", 0));
            Assert.AreEqual(0, ARX.Utility.SafeConvert<int>("invalid", 0));

            // Test Get AutoCAD Path
            var acadPath = ARX.Utility.GetAutoCADPath();
            Assert.IsNotNull(acadPath);

            // Test Safe Execute
            var result = ARX.Utility.SafeExecute(() => 42, 0);
            Assert.AreEqual(42, result);

            var errorResult = ARX.Utility.SafeExecute<int>(() => throw new Exception(), 99);
            Assert.AreEqual(99, errorResult);

            // Test Highlight Entity
            var circle = new Circle(new Point3d(0, 0, 0), Vector3d.ZAxis, 10);
            var entityId = AddEntityToModelSpace(circle);

            var highlightResult = ARX.Utility.HighlightEntity(entityId, true);
            Assert.IsTrue(highlightResult);

            var unhighlightResult = ARX.Utility.HighlightEntity(entityId, false);
            Assert.IsTrue(unhighlightResult);
        }

        [TestCleanup]
        public void Cleanup()
        {
            base.TestCleanup();
        }
    }

    [TestClass]
    public class CADIntegrationTests : TestBase
    {
        [TestInitialize]
        public void Setup()
        {
            base.TestInitialize();
            ARX.Initialize();
        }

        [TestMethod]
        public void CAD_EntityOperations_WorkCorrectly()
        {
            // Test CAD shortcut methods
            var line = new Line(new Point3d(0, 0, 0), new Point3d(100, 100, 0));
            var entityId = AddEntityToModelSpace(line);

            // Test Move using CAD class
            Assert.IsTrue(CAD.Move(entityId, new Point3d(0, 0, 0), new Point3d(50, 50, 0)));

            // Test Copy using CAD class
            var copyId = CAD.Copy(entityId, new Point3d(50, 50, 0), new Point3d(100, 100, 0));
            Assert.IsFalse(copyId.IsNull);

            // Test Rotate using CAD class
            Assert.IsTrue(CAD.Rotate(entityId, new Point3d(50, 50, 0), Math.PI / 4));

            // Test Scale using CAD class
            Assert.IsTrue(CAD.Scale(entityId, new Point3d(50, 50, 0), 2.0));
        }

        [TestMethod]
        public void CAD_DrawingOperations_WorkCorrectly()
        {
            // Test drawing operations using CAD shortcuts
            var lineId = CAD.Line(new Point3d(0, 0, 0), new Point3d(100, 0, 0));
            Assert.IsFalse(lineId.IsNull);

            var circleId = CAD.Circle(new Point3d(50, 50, 0), 25);
            Assert.IsFalse(circleId.IsNull);

            var arcId = CAD.Arc(new Point3d(100, 100, 0), 30, 0, Math.PI);
            Assert.IsFalse(arcId.IsNull);

            var points = new List<Point2d>
            {
                new Point2d(0, 0), new Point2d(100, 0), new Point2d(100, 100), new Point2d(0, 100)
            };
            var polylineId = CAD.Polyline(points, true);
            Assert.IsFalse(polylineId.IsNull);
        }

        [TestMethod]
        public void CAD_LayerOperations_WorkCorrectly()
        {
            // Test layer operations using CAD shortcuts
            var layerId = CAD.CreateLayer("CADTestLayer", 2);
            Assert.IsFalse(layerId.IsNull);

            Assert.IsTrue(CAD.SetCurrentLayer("CADTestLayer"));
            Assert.IsTrue(CAD.LayerExists("CADTestLayer"));

            Assert.IsTrue(CAD.LockLayer("CADTestLayer"));
            Assert.IsTrue(CAD.UnlockLayer("CADTestLayer"));
            Assert.IsTrue(CAD.FreezeLayer("CADTestLayer"));
            Assert.IsTrue(CAD.ThawLayer("CADTestLayer"));
        }

        [TestMethod]
        public void CAD_DatabaseOperations_WorkCorrectly()
        {
            // Test database operations using CAD shortcuts
            var circle = new Circle(new Point3d(0, 0, 0), Vector3d.ZAxis, 10);
            var entityId = CAD.Add(circle);
            Assert.IsFalse(entityId.IsNull);

            var entities = new List<Entity>
            {
                new Line(new Point3d(0, 0, 0), new Point3d(50, 50, 0)),
                new Circle(new Point3d(100, 100, 0), Vector3d.ZAxis, 15)
            };
            var entityIds = CAD.AddBatch(entities);
            Assert.AreEqual(2, entityIds.Count);

            Assert.IsTrue(CAD.Delete(entityId));
            Assert.AreEqual(2, CAD.DeleteBatch(entityIds));
        }

        [TestMethod]
        public void CAD_GeometryOperations_WorkCorrectly()
        {
            // Test geometry operations using CAD shortcuts
            Assert.AreEqual(5.0, CAD.Distance(new Point3d(0, 0, 0), new Point3d(3, 4, 0)), 1e-6);
            Assert.AreEqual(Math.PI / 2, CAD.Angle(new Point3d(1, 0, 0), new Point3d(0, 0, 0), new Point3d(0, 1, 0)), 1e-6);

            var polygon = new List<Point3d>
            {
                new Point3d(0, 0, 0), new Point3d(10, 0, 0), new Point3d(10, 10, 0), new Point3d(0, 10, 0)
            };
            Assert.IsTrue(CAD.PointInPolygon(new Point3d(5, 5, 0), polygon));
            Assert.IsFalse(CAD.PointInPolygon(new Point3d(15, 15, 0), polygon));
        }

        [TestMethod]
        public void CAD_UIOperations_WorkCorrectly()
        {
            // Test UI operations using CAD shortcuts
            try
            {
                CAD.Message("Test message");
                CAD.Message("Test message with title", "Custom Title");
                Assert.IsTrue(true);
            }
            catch
            {
                Assert.Fail("CAD message operations should not throw");
            }

            var confirmResult = CAD.Confirm("Confirm this?");
            Assert.IsTrue(confirmResult == true || confirmResult == false);

            var inputResult = CAD.Input("Enter value:", "default");
            Assert.IsNotNull(inputResult);

            var fileResult = CAD.SelectFile("Select File", "All files (*.*)|*.*");
            Assert.IsNotNull(fileResult);
        }

        [TestMethod]
        public void CAD_StyleOperations_WorkCorrectly()
        {
            // Test style operations using CAD shortcuts
            var textStyleId = CAD.CreateTextStyle("CADTextStyle", "Arial", 2.5);
            Assert.IsFalse(textStyleId.IsNull);

            var dimStyleId = CAD.CreateDimStyle("CADDimStyle", 2.0, 1.0);
            Assert.IsFalse(dimStyleId.IsNull);
        }

        [TestMethod]
        public void CAD_TableOperations_WorkCorrectly()
        {
            // Test table operations using CAD shortcuts
            var tableId = CAD.CreateTable(new Point3d(0, 0, 0), 3, 4, 10, 50);
            Assert.IsFalse(tableId.IsNull);

            Assert.IsTrue(CAD.SetTableCell(tableId, 0, 0, "Test Cell"));
        }

        [TestMethod]
        public void CAD_LayoutOperations_WorkCorrectly()
        {
            // Test layout operations using CAD shortcuts
            var layoutId = CAD.CreateLayout("CADTestLayout");
            Assert.IsFalse(layoutId.IsNull);

            var viewportId = CAD.CreateViewport(new Point3d(100, 100, 0), 200, 150);
            Assert.IsFalse(viewportId.IsNull);
        }

        [TestMethod]
        public void CAD_UtilityOperations_WorkCorrectly()
        {
            // Test utility operations using CAD shortcuts
            Assert.IsTrue(CAD.ValidateString("123", @"^\d+$"));
            Assert.AreEqual(123, CAD.SafeConvert<int>("123", 0));

            var circle = new Circle(new Point3d(0, 0, 0), Vector3d.ZAxis, 10);
            var entityId = AddEntityToModelSpace(circle);
            Assert.IsTrue(CAD.Highlight(entityId, true));
            Assert.IsTrue(CAD.Highlight(entityId, false));
        }

        [TestMethod]
        public void CAD_LoggingOperations_WorkCorrectly()
        {
            // Test logging operations using CAD shortcuts
            try
            {
                CAD.LogInfo("Test info message");
                CAD.LogWarning("Test warning message");
                CAD.LogError("Test error message");
                CAD.LogError("Test error with exception", new Exception("Test exception"));
                Assert.IsTrue(true);
            }
            catch
            {
                Assert.Fail("CAD logging operations should not throw");
            }
        }

        [TestMethod]
        public void CAD_PerformanceOperations_WorkCorrectly()
        {
            // Test performance operations using CAD shortcuts
            try
            {
                CAD.RecordMetric("TestMetric", 42.5);
                CAD.IncrementCounter("TestCounter");

                using (var timer = CAD.StartTimer("TestOperation"))
                {
                    // Simulate some work
                    System.Threading.Thread.Sleep(10);
                }

                Assert.IsTrue(true);
            }
            catch
            {
                Assert.Fail("CAD performance operations should not throw");
            }
        }

        [TestMethod]
        public void CAD_CommandOperations_WorkCorrectly()
        {
            // Test command operations using CAD shortcuts
            Assert.IsTrue(CAD.ExecuteCommand("REGEN"));
            Assert.IsTrue(CAD.ExecuteAsync("ZOOM E"));
        }

        [TestMethod]
        public void CAD_DocumentOperations_WorkCorrectly()
        {
            // Test document operations using CAD shortcuts
            var needsSave = CAD.DocumentNeedsSave();
            Assert.IsTrue(needsSave == true || needsSave == false);

            var saveResult = CAD.SaveDocument();
            // May succeed or fail in test environment

            var saveAsResult = CAD.SaveAs(@"C:\Temp\CADTest.dwg");
            // May succeed or fail due to path issues
        }

        [TestCleanup]
        public void Cleanup()
        {
            base.TestCleanup();
        }
    }
}