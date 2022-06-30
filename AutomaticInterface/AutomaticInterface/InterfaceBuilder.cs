﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutomaticInterface
{
    public record PropertyInfo(string Name, string Ttype, bool HasGet, bool HasSet, string Documentation);

    public record MethodInfo(string Name, string ReturnType, string Documentation, HashSet<string> Parameters, List<(string Arg, string WhereConstraint)> GenericArgs);

    public record EventInfo(string Name, string Type, string Documentation);

    public record Model(string InterfaceName, string Namespace, HashSet<string> Usings, List<PropertyInfo> Properties, List<MethodInfo> Methods, string Documentation, string GenericType, List<EventInfo> Events);

    public class InterfaceBuilder
    {
        private readonly string nameSpaceName;
        private readonly string interfaceName;

        private readonly string autogenerated = @"//--------------------------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
//--------------------------------------------------------------------------------------------------

";

        private readonly HashSet<string> usings = new() { "using System.CodeDom.Compiler;" };
        private readonly List<PropertyInfo> propertyInfos = new();
        private readonly List<MethodInfo> methodInfos = new();
        private readonly List<EventInfo> events = new();
        private string classDocumentation = string.Empty;
        private string genericType = string.Empty;

        public InterfaceBuilder(string nameSpaceName, string interfaceName)
        {
            this.nameSpaceName = nameSpaceName;
            this.interfaceName = interfaceName;
        }

        public void AddPropertyToInterface(string name, string ttype, bool hasGet, bool hasSet, string documentation)
        {
            propertyInfos.Add(new PropertyInfo(name, ttype, hasGet, hasSet, documentation));
        }

        public void AddGeneric(string v)
        {
            genericType = v;
        }

        public void AddClassDocumentation(string documentation)
        {
            classDocumentation = documentation;
        }

        public void AddUsings(IEnumerable<string> usings)
        {
            foreach (var usg in usings)
            {
                this.usings.Add(usg);
            }

        }

        public void AddMethodToInterface(string name, string returnType, string documentation, HashSet<string> parameters, List<(string, string)> genericArgs)
        {
            methodInfos.Add(new MethodInfo(name, returnType, documentation, parameters, genericArgs));
        }

        public void AddEventToInterface(string name, string type, string documentation)
        {
            events.Add(new EventInfo(name, type, documentation));
        }

        public string Build()
        {
            var cb = new CodeBuilder();
            cb.Append(autogenerated);

            foreach (var usg in usings)
            {
                cb.AppendLine(usg);
            }

            cb.AppendLine("");

            cb.AppendLine($"namespace {nameSpaceName}");
            cb.AppendLine("{");

            cb.Indent();

            cb.AppendAndNormalizeMultipleLines(classDocumentation);

            cb.AppendLine($"[GeneratedCode(\"AutomaticInterface\", \"\")]");
            cb.AppendLine($"public partial interface {interfaceName}{genericType}");
            cb.AppendLine("{" );

            cb.Indent();
            foreach (var prop in propertyInfos)
            {
                cb.AppendAndNormalizeMultipleLines(prop.Documentation);
                var get = prop.HasGet ? "get; " : string.Empty;
                var set = prop.HasSet ? "set; " : string.Empty;
                cb.AppendLine($"{prop.Ttype} {prop.Name} {{ {get}{set}}}");
                cb.AppendLine("");
            }
            cb.Dedent();

            cb.Indent();
            foreach (var method in methodInfos)
                BuildMethod(cb, method);

            cb.Dedent();

            cb.Indent();
            foreach (var evt in events)
            {
                cb.AppendAndNormalizeMultipleLines(evt.Documentation);
                cb.AppendLine($"event {evt.Type} {evt.Name};");
                cb.AppendLine("");
            }
            cb.Dedent();

            cb.AppendLine("}");
            cb.Dedent();
            cb.AppendLine("}");

            return cb.Build();
        }

        private static void BuildMethod(CodeBuilder cb, MethodInfo method)
        {
            cb.AppendAndNormalizeMultipleLines(method.Documentation);

            cb.AppendIndented($"{method.ReturnType} {method.Name}");

            if (method.GenericArgs.Any())
                cb.Append($"<{string.Join(", ", method.GenericArgs.Select(a => a.Arg))}>");

            cb.Append($"({string.Join(", ", method.Parameters)})");

            if (method.GenericArgs.Any())
            {
                var constraints = method.GenericArgs
                    .Where(a => !string.IsNullOrWhiteSpace(a.WhereConstraint))
                    .Select(a => a.WhereConstraint);
                cb.Append($" {string.Join(" ", constraints)}");
            }

            cb.Append(";");
            cb.BreakLine();
            cb.AppendLine("");
        }

        public override string ToString()
        {
            return Build();
        }
    }

    public class CodeBuilder
    {
        private StringBuilder sb;
        private int indent;
        private string currentIndent = string.Empty;

        public CodeBuilder() {
            this.sb = new StringBuilder();
            this.indent = 0;
        }

        public void Indent() 
        {
            indent += 4;
            currentIndent = new string(' ', indent);
        }

        public void Dedent()
        {
            indent -= 4;
            currentIndent = new string(' ', indent);
        }

        public void BreakLine()
        {
            sb.AppendLine();
        }

        public void AppendIndented(string str)
        {
            sb.Append(' ', indent);
            sb.Append(str);
        }

        public void AppendLine(string str)
        {
            sb.Append(' ', indent);
            sb.AppendLine(str);
        }

        public void Append(string str)
        {
            sb.Append(str);
        }

        public void AppendAndNormalizeMultipleLines(string doc)
        {
            if (!string.IsNullOrWhiteSpace(doc))
            {
                foreach (var line in doc.SplitToLines())
                {
                    sb.AppendLine(indentStr(line));
                }
            }
        }

        private string indentStr(string str)
        {
            return str.TrimStart().Insert(0, currentIndent);
        }

        public string Build()
        {
            return sb.ToString();
        }
    }
}
