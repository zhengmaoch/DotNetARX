﻿﻿// AutoCAD
global using Autodesk.AutoCAD.ApplicationServices;
global using Autodesk.AutoCAD.Colors;
global using Autodesk.AutoCAD.DatabaseServices;
global using Autodesk.AutoCAD.EditorInput;
global using Autodesk.AutoCAD.Geometry;
global using Autodesk.AutoCAD.Runtime;
// DotNetARX Core
global using DotNetARX.Async;
global using DotNetARX.Caching;
global using DotNetARX.CodeGeneration;
global using DotNetARX.Configuration;
global using DotNetARX.DependencyInjection;
global using DotNetARX.Diagnostics;
global using DotNetARX.Events;
global using DotNetARX.Exceptions;
global using DotNetARX.Extensions;
global using DotNetARX.Interfaces;
global using DotNetARX.Logging;
global using DotNetARX.Performance;
global using DotNetARX.ResourceManagement;
global using DotNetARX.Services;
global using DotNetARX.Testing;
global using DotNetARX.Models;

// Testing
global using Moq;
global using Microsoft.CodeAnalysis;
global using Microsoft.VisualStudio.TestTools.UnitTesting;

// System
global using System;
global using System.Collections.Generic;
global using System.Drawing.Printing;
global using System.Linq;
global using System.Reflection;
global using System.Runtime.InteropServices;

// DotNetARX Tests
global using DotNetARX.Tests;

// DotNetARX 统一API - 易用性与高性能的完美结合

// Default namespace
global using Exception = System.Exception;
global using Polyline = Autodesk.AutoCAD.DatabaseServices.Polyline;
global using Viewport = Autodesk.AutoCAD.DatabaseServices.Viewport;