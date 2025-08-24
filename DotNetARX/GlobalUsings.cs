// AutoCAD
// System
global using Autodesk.AutoCAD.ApplicationServices;
global using Autodesk.AutoCAD.DatabaseServices;
global using Autodesk.AutoCAD.EditorInput;
global using Autodesk.AutoCAD.Geometry;
global using Autodesk.AutoCAD.Runtime;

// DotNetARX Core
global using DotNetARX.Configuration;
global using DotNetARX.Exceptions;
global using DotNetARX.Logging;
global using DotNetARX.ResourceManagement;
global using DotNetARX.Performance;
global using DotNetARX.Diagnostics;
global using DotNetARX.CodeGeneration;

// DotNetARX 统一API - 易用性与高性能的完美结合
global using static DotNetARX.CAD;
global using System;
global using System.Collections.Concurrent;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Reflection;
global using System.Runtime.InteropServices;
global using System.Text;
global using System.Text.RegularExpressions;
global using System.Threading.Tasks;
global using System.Threading;

// Default namespace
global using Exception = System.Exception;
global using Polyline = Autodesk.AutoCAD.DatabaseServices.Polyline;
global using Viewport = Autodesk.AutoCAD.DatabaseServices.Viewport;