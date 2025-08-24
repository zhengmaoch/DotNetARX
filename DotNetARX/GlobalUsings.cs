// AutoCAD
// System
global using Autodesk.AutoCAD.ApplicationServices;
global using Autodesk.AutoCAD.Colors;
global using Autodesk.AutoCAD.DatabaseServices;
global using Autodesk.AutoCAD.EditorInput;
global using Autodesk.AutoCAD.Geometry;
global using Autodesk.AutoCAD.Runtime;
global using DotNetARX.CodeGeneration;

// DotNetARX Core
global using DotNetARX.Configuration;
global using DotNetARX.Diagnostics;
global using DotNetARX.Exceptions;
global using DotNetARX.Logging;
global using DotNetARX.Performance;
global using DotNetARX.ResourceManagement;
global using System;
global using System.Collections.Concurrent;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Reflection;
global using System.Runtime.InteropServices;
global using System.Text;
global using System.Text.RegularExpressions;
global using System.Threading;
global using System.Threading.Tasks;

// DotNetARX 统一API - 易用性与高性能的完美结合
global using static DotNetARX.CAD;

// Default namespace
global using Application = Autodesk.AutoCAD.ApplicationServices.Application;
global using Color = Autodesk.AutoCAD.Colors.Color;
global using Exception = System.Exception;
global using Polyline = Autodesk.AutoCAD.DatabaseServices.Polyline;
global using Viewport = Autodesk.AutoCAD.DatabaseServices.Viewport;