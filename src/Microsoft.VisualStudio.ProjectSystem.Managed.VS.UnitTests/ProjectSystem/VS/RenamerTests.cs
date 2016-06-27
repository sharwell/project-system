﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [ProjectSystemTrait]
    public class RenamerTests
    {
        bool ConfirmForRenameWasCalled { get; set; }
        bool RenameServiceWasCalled { get; set; }
        bool ApplySolutionWasCalled { get; set; }

        [Theory]
        [InlineData("class Foo{}", "Foo.cs", "Bar.cs")]
        [InlineData("interface Foo { void m1(); void m2();}", "Foo.cs", "Bar.cs")]
        [InlineData("delegate int Foo(string s);", "Foo.cs", "Bar.cs")]
        [InlineData("partial class Foo {} partial class Foo {}", "Foo.cs", "Bar.cs")]
        [InlineData("struct Foo { decimal price; string title; string author;}", "Foo.cs", "Bar.cs" )]
        [InlineData("enum Foo { None, enum1, enum2, enum3, enum4 };", "Foo.cs", "Bar.cs")]
        [InlineData("namespace n1 {class Foo{}} namespace n2 {class Foo{}}", "Foo.cs", "Bar.cs")]
        public async Task Rename_Symbol_Should_TriggerUserConfirmationAsync(string soureCode, string oldFilePath, string newFilePath)
        {
            var userNotificationServices = IUserNotificationServicesFactory.Implement(f =>
            {
                ConfirmForRenameWasCalled = true;
                return false;
            });
            await RenameAsync(soureCode, oldFilePath, newFilePath, userNotificationServices);
            Assert.True(ConfirmForRenameWasCalled);
            Assert.False(RenameServiceWasCalled);
        }

        [Theory]
        [InlineData("class Foo{}", "Foo.cs", "Bar.cs")]
        [InlineData("interface Foo { void m1(); void m2();}", "Foo.cs", "Bar.cs")]
        [InlineData("delegate int Foo(string s);", "Foo.cs", "Bar.cs")]
        [InlineData("partial class Foo {} partial class Foo {}", "Foo.cs", "Bar.cs")]
        [InlineData("struct Foo { decimal price; string title; string author;}", "Foo.cs", "Bar.cs")]
        [InlineData("enum Foo { None, enum1, enum2, enum3, enum4 };", "Foo.cs", "Bar.cs")]
        [InlineData("namespace n1 {class Foo{}} namespace n2 {class Foo{}}", "Foo.cs", "Bar.cs")]
        public async Task Rename_Symbol_Should_HappenAsync(string soureCode, string oldFilePath, string newFilePath)
        {
            var userNotificationServices = IUserNotificationServicesFactory.Implement(f =>
            {
                ConfirmForRenameWasCalled = true;
                return true;
            });
            await RenameAsync(soureCode, oldFilePath, newFilePath, userNotificationServices);
            Assert.True(ConfirmForRenameWasCalled);
            Assert.True(RenameServiceWasCalled);
            Assert.True(ApplySolutionWasCalled);
        }

        [Theory]
        [InlineData("class Foo{}", "Bar.cs", "Foo.cs")]
        [InlineData("class Foo1{}", "Foo.cs", "Bar.cs")]
        [InlineData("interface Foo1 { void m1(); void m2();}", "Foo.cs", "Bar.cs")]
        [InlineData("delegate int Foo1(string s);", "Foo.cs", "Bar.cs")]
        [InlineData("partial class Foo1 {} partial class Foo2 {}", "Foo.cs", "Bar.cs")]
        [InlineData("struct Foo1 { decimal price; string title; string author;}", "Foo.cs", "Bar.cs")]
        [InlineData("enum Foo1 { None, enum1, enum2, enum3, enum4 };", "Foo.cs", "Bar.cs")]
        public async Task Rename_Symbol_Should_Not_HappenAsync(string soureCode, string oldFilePath, string newFilePath)
        {
            var userNotificationServices = IUserNotificationServicesFactory.Implement(f => 
            {
                ConfirmForRenameWasCalled = true;
                return false;
            });

            await RenameAsync(soureCode, oldFilePath, newFilePath, userNotificationServices);
            Assert.False(ConfirmForRenameWasCalled);
            Assert.False(RenameServiceWasCalled);
            Assert.False(ApplySolutionWasCalled);
        }

        private async Task RenameAsync(string soureCode, string oldFilePath, string newFilePath, IUserNotificationServices userNotificationServices)
        {
            using (var ws = new AdhocWorkspace())
            {
                var projectId = ProjectId.CreateNewId();
                Solution solution = ws.AddSolution(InitializeWorkspace(projectId, newFilePath, soureCode));
                Project project = (from d in solution.Projects where d.Id == projectId select d).FirstOrDefault();

                var roslynServices = IRoslynServicesFactory.Implement(async(s, a, n) => await RenameSymbolAsync(s, a, n, projectId, newFilePath),(w,s) => ApplyChangesToSolution(w,s));
                var optionsSettingsFactory = IOptionsSettingsFactory.Implement((string category, string page, string property, bool defaultValue) => { return true; });
                
                var renamer = new Renamer(ws, IProjectThreadingServiceFactory.Create(), userNotificationServices, optionsSettingsFactory, roslynServices, project, oldFilePath, newFilePath);
                await renamer.RenameAsync(project);
            }
        }

        private async Task<Solution> RenameSymbolAsync(Solution solution, ISymbol symbol, string newName, ProjectId projectId, string newFilePath)
        {
            using (var ws = new AdhocWorkspace())
            {
                RenameServiceWasCalled = true;

                Solution _solution = ws.AddSolution(InitializeWorkspace(projectId, newFilePath, "class temp{}"));
                return await Task.FromResult(_solution);
            }
        }

        private bool ApplyChangesToSolution(Workspace ws, Solution renamedSolution)
        {
            ApplySolutionWasCalled = true;
            return true;
        }

        private SolutionInfo InitializeWorkspace(ProjectId projectId, string fileName, string code)
        {
            var solutionId = SolutionId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);
            return SolutionInfo.Create(
                solutionId,
                VersionStamp.Create(),
                projects: new ProjectInfo[]
                {
                    ProjectInfo.Create(
                        id: projectId,
                        version: VersionStamp.Create(),
                        name: "Project1",
                        assemblyName: "Project1",
                        filePath: @"C:\project1.csproj",
                        language: LanguageNames.CSharp,
                        documents: new DocumentInfo[]
                        {
                                DocumentInfo.Create(
                                documentId,
                                fileName,
                                loader: TextLoader.From(TextAndVersion.Create(SourceText.From(code),
                                VersionStamp.Create())),
                                filePath: fileName)
                         })
                });
        }
    }
}
