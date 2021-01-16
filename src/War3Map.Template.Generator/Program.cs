using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.IO;
using War3Net.Build.Environment;
using War3Net.Build.Extensions;
using War3Net.CodeAnalysis.CSharp;
using War3Net.CodeAnalysis.Jass;
using War3Net.CodeAnalysis.Jass.Syntax;
using War3Net.CodeAnalysis.Transpilers;

namespace War3Map.Template.Generator
{
    internal static class Program
    {
        private const string GeneratedCodeProjectFolderPath = @"..\..\..\..\War3Map.Template.Generated";

        private static void Main(string[] args)
        {
#if false
            var mapRegions = new MapRegions(MapRegionsFormatVersion.Normal);
            mapRegions.Regions.Add(new Region
            {
                Left = -2594f,
                Right = -2016f,
                Bottom = 992f,
                Top = 1568f,
                Name = "Spawn1Reg",
            });
#else
            var mapRegionsPath = ""; // TODO

            using var mapRegionsFileStream = File.OpenRead(mapRegionsPath);
            using var mapRegionsReader = new BinaryReader(mapRegionsFileStream);

            var mapRegions = mapRegionsReader.ReadMapRegions();
#endif

            var members = new List<MemberDeclarationSyntax>();
            foreach (var region in mapRegions.Regions)
            {
                var decl = new GlobalVariableDeclarationSyntax(
                    new War3Net.CodeAnalysis.Jass.Syntax.VariableDeclarationSyntax(
                        new VariableDefinitionSyntax(
                            JassSyntaxFactory.ParseTypeName("rect"),
                            JassSyntaxFactory.Token(SyntaxTokenType.AlphanumericIdentifier, $"gg_rct_{region.Name.Replace(' ', '_')}"),
                            JassSyntaxFactory.Empty())),
                    JassSyntaxFactory.Newlines());

                members.Add(JassToCSharpTranspiler.Transpile(decl));
            }

            var @class = JassTranspilerHelper.GetClassDeclaration("Regions", members, true);
            var @namespace = JassTranspilerHelper.GetNamespaceDeclaration("War3Map.Template.Generated", @class);
            var usingDirectives = new UsingDirectiveSyntax[]
            {
                SyntaxFactory.UsingDirective(SyntaxFactory.Token(SyntaxKind.StaticKeyword), null, SyntaxFactory.ParseName("War3Api.Common")),
            };

            var compilationUnit = JassTranspilerHelper.GetCompilationUnit(new SyntaxList<UsingDirectiveSyntax>(usingDirectives), @namespace);

            var path = Path.Combine(GeneratedCodeProjectFolderPath, "Regions.cs");
            using var fileStream = File.Create(path);
            CompilationHelper.SerializeTo(compilationUnit, fileStream);
        }
    }
}