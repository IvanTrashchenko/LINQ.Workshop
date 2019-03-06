// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
//
//Copyright (C) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

public class ObjectDumper
{
    int depth;

    int level;

    int pos;

    TextWriter writer;

    private ObjectDumper(int depth)
    {
        this.writer = Console.Out;
        this.depth = depth;
    }

    public static void Write(object o)
    {
        Write(o, 0);
    }

    public static void Write(object o, int depth)
    {
        ObjectDumper dumper = new ObjectDumper(depth);
        dumper.WriteObject(null, o);
    }

    private void Write(string s)
    {
        if (s != null)
        {
            this.writer.Write(s);
            this.pos += s.Length;
        }
    }

    private void WriteIndent()
    {
        for (int i = 0; i < this.level; i++) this.writer.Write("  ");
    }

    private void WriteLine()
    {
        this.writer.WriteLine();
        this.pos = 0;
    }

    private void WriteObject(string prefix, object o)
    {
        if (o == null || o is ValueType || o is string)
        {
            this.WriteIndent();
            this.Write(prefix);
            this.WriteValue(o);
            this.WriteLine();
        }
        else if (o is IEnumerable)
        {
            foreach (object element in (IEnumerable)o)
            {
                if (element is IEnumerable && !(element is string))
                {
                    this.WriteIndent();
                    this.Write(prefix);
                    this.Write("...");
                    this.WriteLine();
                    if (this.level < this.depth)
                    {
                        this.level++;
                        this.WriteObject(prefix, element);
                        this.level--;
                    }
                }
                else
                {
                    this.WriteObject(prefix, element);
                }
            }
        }
        else
        {
            MemberInfo[] members = o.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance);
            this.WriteIndent();
            this.Write(prefix);
            bool propWritten = false;
            foreach (MemberInfo m in members)
            {
                FieldInfo f = m as FieldInfo;
                PropertyInfo p = m as PropertyInfo;
                if (f != null || p != null)
                {
                    if (propWritten)
                    {
                        this.WriteTab();
                    }
                    else
                    {
                        propWritten = true;
                    }

                    this.Write(m.Name);
                    this.Write("=");
                    Type t = f != null ? f.FieldType : p.PropertyType;
                    if (t.IsValueType || t == typeof(string))
                    {
                        this.WriteValue(f != null ? f.GetValue(o) : p.GetValue(o, null));
                    }
                    else
                    {
                        if (typeof(IEnumerable).IsAssignableFrom(t))
                        {
                            this.Write("...");
                        }
                        else
                        {
                            this.Write("{ }");
                        }
                    }
                }
            }

            if (propWritten) this.WriteLine();
            if (this.level < this.depth)
            {
                foreach (MemberInfo m in members)
                {
                    FieldInfo f = m as FieldInfo;
                    PropertyInfo p = m as PropertyInfo;
                    if (f != null || p != null)
                    {
                        Type t = f != null ? f.FieldType : p.PropertyType;
                        if (!(t.IsValueType || t == typeof(string)))
                        {
                            object value = f != null ? f.GetValue(o) : p.GetValue(o, null);
                            if (value != null)
                            {
                                this.level++;
                                this.WriteObject(m.Name + ": ", value);
                                this.level--;
                            }
                        }
                    }
                }
            }
        }
    }

    private void WriteTab()
    {
        this.Write("  ");
        while (this.pos % 8 != 0) this.Write(" ");
    }

    private void WriteValue(object o)
    {
        if (o == null)
        {
            this.Write("null");
        }
        else if (o is DateTime)
        {
            this.Write(((DateTime)o).ToShortDateString());
        }
        else if (o is ValueType || o is string)
        {
            this.Write(o.ToString());
        }
        else if (o is IEnumerable)
        {
            this.Write("...");
        }
        else
        {
            this.Write("{ }");
        }
    }
}