using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using War3Net.Build.Environment;
using War3Net.Build.Extensions;
using War3Net.CodeAnalysis.CSharp;
using War3Net.CodeAnalysis.Jass;
using War3Net.CodeAnalysis.Jass.Syntax;
using War3Net.CodeAnalysis.Transpilers;
using War3Net.IO.Mpq;

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

            // var mapArchivePath = ""; // TODO
            // using var mapRegionsFileStream = MpqFile.OpenRead(Path.Combine(mapArchivePath, MapRegions.FileName));

            using var mapRegionsReader = new BinaryReader(mapRegionsFileStream);

            var mapRegions = mapRegionsReader.ReadMapRegions();
#endif

            var members = new List<MemberDeclarationSyntax>();
            var luamembers = new List<MemberDeclarationSyntax>();
            foreach (var region in mapRegions.Regions)
            {
                if (string.IsNullOrEmpty(region.AmbientSound) && region.WeatherType == 0)
                { 
                    var decl = new GlobalVariableDeclarationSyntax(
                        new War3Net.CodeAnalysis.Jass.Syntax.VariableDeclarationSyntax(
                            new VariableDefinitionSyntax(
                                JassSyntaxFactory.ParseTypeName("rect"),
                                JassSyntaxFactory.Token(SyntaxTokenType.AlphanumericIdentifier, $"gg_rct_{region.Name.Replace(' ', '_')}"),
                                JassSyntaxFactory.EqualsValueClause(JassSyntaxFactory.InvocationExpression(
                                    "Rect",
                                    JassSyntaxFactory.ConstantExpression(region.Left),
                                    JassSyntaxFactory.ConstantExpression(region.Bottom),
                                    JassSyntaxFactory.ConstantExpression(region.Right),
                                    JassSyntaxFactory.ConstantExpression(region.Top))))),
                        JassSyntaxFactory.Newlines());
                    
                    members.Add(JassToCSharpTranspiler.Transpile(decl));
                }
                else
                {
                    var decl = new GlobalVariableDeclarationSyntax(
                        new War3Net.CodeAnalysis.Jass.Syntax.VariableDeclarationSyntax(
                            new VariableDefinitionSyntax(
                                JassSyntaxFactory.ParseTypeName("rect"),
                                JassSyntaxFactory.Token(SyntaxTokenType.AlphanumericIdentifier, $"gg_rct_{region.Name.Replace(' ', '_')}"),
                                JassSyntaxFactory.Empty())),
                        JassSyntaxFactory.Newlines());

                    luamembers.Add(JassToCSharpTranspiler.Transpile(decl));
                }
            }

            var class1 = JassTranspilerHelper.GetClassDeclaration("Regions", members, false);
            var class2 = JassTranspilerHelper.GetClassDeclaration("LuaRegions", luamembers, true);
            var usingDirectives = new UsingDirectiveSyntax[]
            {
                SyntaxFactory.UsingDirective(SyntaxFactory.Token(SyntaxKind.StaticKeyword), null, SyntaxFactory.ParseName("War3Api.Common")),
            };

            var compilationUnit = JassTranspilerHelper.GetCompilationUnit(
                new SyntaxList<UsingDirectiveSyntax>(usingDirectives),
                JassTranspilerHelper.GetNamespaceDeclaration("War3Map.Template.Generated", class1.Concat(class2).ToArray()));

            var path = Path.Combine(GeneratedCodeProjectFolderPath, "Regions.cs");
            using var fileStream = File.Create(path);
            CompilationHelper.SerializeTo(compilationUnit, fileStream);
        }
    }
}