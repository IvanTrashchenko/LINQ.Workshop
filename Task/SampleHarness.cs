// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
//
//Copyright (C) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace SampleSupport
{
    public class SampleHarness : IEnumerable<Sample>
    {
        private readonly StreamWriter _OutputStreamWriter = new StreamWriter(new MemoryStream());

        private readonly string _Title;

        private readonly IDictionary<int, Sample> samples = new Dictionary<int, Sample>();

        public SampleHarness()
        {
            Type samplesType = this.GetType();

            this._Title = "Samples";
            string prefix = "Sample";
            string codeFile = samplesType.Name + ".cs";

            foreach (Attribute a in samplesType.GetCustomAttributes(false))
            {
                if (a is TitleAttribute) this._Title = ((TitleAttribute)a).Title;
                else if (a is PrefixAttribute)
                    prefix = ((PrefixAttribute)a).Prefix;
            }

            String allCode = readFile(Application.StartupPath + @"\..\..\" + codeFile);

            var methods =
                from sm in samplesType.GetMethods(
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Static)
                where sm.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                orderby sm.MetadataToken
                select sm;

            int m = 1;
            foreach (var method in methods)
            {
                string methodCategory = "Miscellaneous";
                string methodTitle = prefix + " Sample " + m;
                string methodDescription = "See code.";
                List<MethodInfo> linkedMethods = new List<MethodInfo>();
                List<Type> linkedClasses = new List<Type>();

                foreach (Attribute a in method.GetCustomAttributes(false))
                {
                    if (a is CategoryAttribute)
                        methodCategory = ((CategoryAttribute)a).Category;
                    else if (a is TitleAttribute)
                        methodTitle = ((TitleAttribute)a).Title;
                    else if (a is DescriptionAttribute)
                        methodDescription = ((DescriptionAttribute)a).Description;
                    else if (a is LinkedMethodAttribute)
                    {
                        MethodInfo linked = samplesType.GetMethod(
                            ((LinkedMethodAttribute)a).MethodName,
                            (BindingFlags.Public | BindingFlags.NonPublic)
                            | (BindingFlags.Static | BindingFlags.Instance));
                        if (linked != null)
                            linkedMethods.Add(linked);
                    }
                    else if (a is LinkedClassAttribute)
                    {
                        Type linked = samplesType.GetNestedType(((LinkedClassAttribute)a).ClassName);
                        if (linked != null)
                            linkedClasses.Add(linked);
                    }
                }

                StringBuilder methodCode = new StringBuilder();
                methodCode.Append(getCodeBlock(allCode, "void " + method.Name));

                foreach (MethodInfo lm in linkedMethods)
                {
                    methodCode.Append(Environment.NewLine);
                    methodCode.Append(getCodeBlock(allCode, shortTypeName(lm.ReturnType.FullName) + " " + lm.Name));
                }

                foreach (Type lt in linkedClasses)
                {
                    methodCode.Append(Environment.NewLine);
                    methodCode.Append(getCodeBlock(allCode, "class " + lt.Name));
                }

                Sample sample = new Sample(
                    this,
                    method,
                    methodCategory,
                    methodTitle,
                    methodDescription,
                    methodCode.ToString());

                this.samples.Add(m, sample);
                m++;
            }
        }

        public StreamWriter OutputStreamWriter
        {
            get
            {
                return this._OutputStreamWriter;
            }
        }

        public string Title
        {
            get
            {
                return this._Title;
            }
        }

        public Sample this[int index]
        {
            get
            {
                return this.samples[index];
            }
        }

        public virtual void HandleException(Exception e)
        {
            Console.Write(e);
        }

        public virtual void InitSample()
        {
        }

        public void RunAllSamples()
        {
            TextWriter oldConsoleOut = Console.Out;
            Console.SetOut(StreamWriter.Null);

            foreach (Sample sample in this)
            {
                sample.Invoke();
            }

            Console.SetOut(oldConsoleOut);
        }

        IEnumerator<Sample> IEnumerable<Sample>.GetEnumerator()
        {
            return this.samples.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.samples.Values.GetEnumerator();
        }

        private static string getCodeBlock(string allCode, string blockName)
        {
            int blockStart = allCode.IndexOf(blockName, StringComparison.OrdinalIgnoreCase);

            if (blockStart == -1)
                return "// " + blockName + " code not found";
            blockStart = allCode.LastIndexOf(Environment.NewLine, blockStart, StringComparison.OrdinalIgnoreCase);
            if (blockStart == -1)
                blockStart = 0;
            else
                blockStart += Environment.NewLine.Length;

            int pos = blockStart;
            int braceCount = 0;
            char c;
            do
            {
                pos++;

                c = allCode[pos];
                switch (c)
                {
                    case '{':
                        braceCount++;
                        break;

                    case '}':
                        braceCount--;
                        break;
                }
            }
            while (pos < allCode.Length && !(c == '}' && braceCount == 0));

            int blockEnd = pos;

            string blockCode = allCode.Substring(blockStart, blockEnd - blockStart + 1);

            return removeIndent(blockCode);
        }

        private static String readFile(string path)
        {
            String fileContents;
            if (File.Exists(path))
                using (StreamReader reader = File.OpenText(path))
                    fileContents = reader.ReadToEnd();
            else
                fileContents = "";

            return fileContents;
        }

        private static string removeIndent(string code)
        {
            int indentSpaces = 0;
            while (code[indentSpaces] == ' ')
            {
                indentSpaces++;
            }

            StringBuilder builder = new StringBuilder();
            string[] codeLines = code.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            foreach (string line in codeLines)
            {
                if (indentSpaces < line.Length)
                    builder.AppendLine(line.Substring(indentSpaces));
                else
                    builder.AppendLine();
            }

            return builder.ToString();
        }

        private static string shortTypeName(string typeName)
        {
            bool isAssemblyQualified = typeName[0] == '[';
            if (isAssemblyQualified)
            {
                int commaPos = typeName.IndexOf(',');
                return shortTypeName(typeName.Substring(1, commaPos - 1));
            }
            else
            {
                bool isGeneric = typeName.Contains("`");
                if (isGeneric)
                {
                    int backTickPos = typeName.IndexOf('`');
                    int leftBracketPos = typeName.IndexOf('[');
                    string typeParam = shortTypeName(
                        typeName.Substring(leftBracketPos + 1, typeName.Length - leftBracketPos - 2));
                    return shortTypeName(typeName.Substring(0, backTickPos)) + "<" + typeParam + ">";
                }
                else
                {
                    switch (typeName)
                    {
                        case "System.Void": return "void";
                        case "System.Int16": return "short";
                        case "System.Int32": return "int";
                        case "System.Int64": return "long";
                        case "System.Single": return "float";
                        case "System.Double": return "double";
                        case "System.String": return "string";
                        case "System.Char": return "char";
                        case "System.Boolean": return "bool";

                        /* other primitive types omitted */

                        default:
                            int lastDotPos = typeName.LastIndexOf('.');
                            int lastPlusPos = typeName.LastIndexOf('+');
                            int startPos = Math.Max(lastDotPos, lastPlusPos) + 1;
                            return typeName.Substring(startPos, typeName.Length - startPos);
                    }
                }
            }
        }
    }

    public class Sample
    {
        private readonly string _Category;

        private readonly string _Code;

        private readonly string _Description;

        private readonly SampleHarness _Harness;

        private readonly MethodInfo _Method;

        private readonly string _Title;

        public Sample(
            SampleHarness harness,
            MethodInfo method,
            string category,
            string title,
            string description,
            string code)
        {
            this._Harness = harness;
            this._Method = method;
            this._Category = category;
            this._Title = title;
            this._Description = description;
            this._Code = code;
        }

        public string Category
        {
            get
            {
                return this._Category;
            }
        }

        public string Code
        {
            get
            {
                return this._Code;
            }
        }

        public string Description
        {
            get
            {
                return this._Description;
            }
        }

        public SampleHarness Harness
        {
            get
            {
                return this._Harness;
            }
        }

        public MethodInfo Method
        {
            get
            {
                return this._Method;
            }
        }

        public string Title
        {
            get
            {
                return this._Title;
            }
        }

        public void Invoke()
        {
            this._Harness.InitSample();
            this._Method.Invoke(this._Harness, null);
        }

        public void InvokeSafe()
        {
            try
            {
                this.Invoke();
            }
            catch (TargetInvocationException e)
            {
                this._Harness.HandleException(e.InnerException);
            }
        }

        public override string ToString()
        {
            return this.Title;
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public sealed class TitleAttribute : Attribute
    {
        public TitleAttribute(string title)
        {
            this.Title = title;
        }

        public string Title { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public sealed class PrefixAttribute : Attribute
    {
        public PrefixAttribute(string prefix)
        {
            this.Prefix = prefix;
        }

        public string Prefix { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class CategoryAttribute : Attribute
    {
        public CategoryAttribute(string category)
        {
            this.Category = category;
        }

        public string Category { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class DescriptionAttribute : Attribute
    {
        public DescriptionAttribute(string description)
        {
            this.Description = description;
        }

        public string Description { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class LinkedMethodAttribute : Attribute
    {
        public LinkedMethodAttribute(string methodName)
        {
            this.MethodName = methodName;
        }

        public string MethodName { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class LinkedClassAttribute : Attribute
    {
        public LinkedClassAttribute(string className)
        {
            this.ClassName = className;
        }

        public string ClassName { get; set; }
    }
}