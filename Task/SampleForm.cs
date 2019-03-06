// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
//
//Copyright (C) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace SampleSupport
{
    internal partial class SampleForm : Form
    {
        private SampleHarness currentHarness;

        private Sample currentSample;

        public SampleForm(string title, List<SampleHarness> harnesses)
        {
            this.InitializeComponent();

            this.Text = title;

            TreeNode rootNode = new TreeNode(title);
            rootNode.Tag = null;
            rootNode.ImageKey = "Help";
            rootNode.SelectedImageKey = "Help";
            this.samplesTreeView.Nodes.Add(rootNode);
            rootNode.Expand();

            foreach (SampleHarness harness in harnesses)
            {
                TreeNode harnessNode = new TreeNode(harness.Title);
                harnessNode.Tag = null;
                harnessNode.ImageKey = "BookStack";
                harnessNode.SelectedImageKey = "BookStack";
                rootNode.Nodes.Add(harnessNode);

                string category = string.Empty;
                TreeNode categoryNode = null;
                foreach (Sample sample in harness)
                {
                    if (sample.Category != category)
                    {
                        category = sample.Category;

                        categoryNode = new TreeNode(category);
                        categoryNode.Tag = null;
                        categoryNode.ImageKey = "BookClosed";
                        categoryNode.SelectedImageKey = "BookClosed";
                        harnessNode.Nodes.Add(categoryNode);
                    }

                    TreeNode node = new TreeNode(sample.ToString());
                    node.Tag = sample;
                    node.ImageKey = "Item";
                    node.SelectedImageKey = "Item";
                    categoryNode.Nodes.Add(node);
                }
            }
        }

        private static void colorizeCode(RichTextBox rtb)
        {
            string[] keywords =
                {
                    "as", "do", "if", "in", "is", "for", "int", "new", "out", "ref", "try", "base", "bool", "byte",
                    "case", "char", "else", "enum", "goto", "lock", "long", "null", "this", "true", "uint", "void",
                    "break", "catch", "class", "const", "event", "false", "fixed", "float", "sbyte", "short", "throw",
                    "ulong", "using", "where", "while", "yield", "double", "extern", "object", "params", "public",
                    "return", "sealed", "sizeof", "static", "string", "struct", "switch", "typeof", "unsafe", "ushort",
                    "checked", "decimal", "default", "finally", "foreach", "partial", "private", "virtual", "abstract",
                    "continue", "delegate", "explicit", "implicit", "internal", "operator", "override", "readonly",
                    "volatile", "interface", "namespace", "protected", "unchecked", "stackalloc", "from", "in", "where",
                    "select", "join", "equals", "let", "on", "group", "by", "into", "orderby", "ascending",
                    "descending", "var"
                };
            string text = rtb.Text;

            rtb.SelectAll();
            rtb.SelectionColor = rtb.ForeColor;

            foreach (string keyword in keywords)
            {
                int keywordPos = rtb.Find(keyword, RichTextBoxFinds.MatchCase | RichTextBoxFinds.WholeWord);
                while (keywordPos != -1)
                {
                    int commentPos = text.LastIndexOf("//", keywordPos, StringComparison.OrdinalIgnoreCase);
                    int newLinePos = text.LastIndexOf("\n", keywordPos, StringComparison.OrdinalIgnoreCase);
                    int quoteCount = 0;
                    int quotePos = text.IndexOf(
                        "\"",
                        newLinePos + 1,
                        keywordPos - newLinePos,
                        StringComparison.OrdinalIgnoreCase);
                    while (quotePos != -1)
                    {
                        quoteCount++;
                        quotePos = text.IndexOf(
                            "\"",
                            quotePos + 1,
                            keywordPos - (quotePos + 1),
                            StringComparison.OrdinalIgnoreCase);
                    }

                    if (newLinePos >= commentPos && quoteCount % 2 == 0)
                        rtb.SelectionColor = Color.Blue;

                    keywordPos = rtb.Find(
                        keyword,
                        keywordPos + rtb.SelectionLength,
                        RichTextBoxFinds.MatchCase | RichTextBoxFinds.WholeWord);
                }
            }

            rtb.Select(0, 0);
        }

        private void runButton_Click(object sender, EventArgs e)
        {
            this.RunCurrentSample();
        }

        private void RunCurrentSample()
        {
            Cursor hold = this.Cursor;
            this.Cursor = Cursors.WaitCursor;

            this.outputTextBox.Text = string.Empty;
            StreamWriter writer = this.currentHarness.OutputStreamWriter;
            TextWriter oldConsoleOut = Console.Out;
            Console.SetOut(writer);
            MemoryStream stream = (MemoryStream)writer.BaseStream;
            stream.SetLength(0);

            this.currentSample.InvokeSafe();

            writer.Flush();
            Console.SetOut(oldConsoleOut);
            this.outputTextBox.Text += writer.Encoding.GetString(stream.ToArray());

            this.Cursor = hold;
        }

        private void samplesTreeView_AfterCollapse(object sender, TreeViewEventArgs e)
        {
            switch (e.Node.Level)
            {
                case 1:
                    e.Node.ImageKey = "BookStack";
                    e.Node.SelectedImageKey = "BookStack";
                    break;

                case 2:
                    e.Node.ImageKey = "BookClosed";
                    e.Node.SelectedImageKey = "BookClosed";
                    break;
            }
        }

        private void samplesTreeView_AfterExpand(object sender, TreeViewEventArgs e)
        {
            switch (e.Node.Level)
            {
                case 1:
                case 2:
                    e.Node.ImageKey = "BookOpen";
                    e.Node.SelectedImageKey = "BookOpen";
                    break;
            }
        }

        private void samplesTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            TreeNode currentNode = this.samplesTreeView.SelectedNode;
            this.currentSample = (Sample)currentNode.Tag;
            if (this.currentSample != null)
            {
                this.currentHarness = this.currentSample.Harness;
                this.runButton.Enabled = true;
                this.descriptionTextBox.Text = this.currentSample.Description;
                this.codeRichTextBox.Clear();
                this.codeRichTextBox.Text = this.currentSample.Code;
                colorizeCode(this.codeRichTextBox);
                this.outputTextBox.Clear();
            }
            else
            {
                this.currentHarness = null;
                this.runButton.Enabled = false;
                this.descriptionTextBox.Text = "Select a query from the tree to the left.";
                this.codeRichTextBox.Clear();
                this.outputTextBox.Clear();
                if (e.Action != TreeViewAction.Collapse && e.Action != TreeViewAction.Unknown)
                    e.Node.Expand();
            }
        }

        private void samplesTreeView_BeforeCollapse(object sender, TreeViewCancelEventArgs e)
        {
            switch (e.Node.Level)
            {
                case 0:
                    e.Cancel = true;
                    break;
            }
        }

        private void samplesTreeView_DoubleClick(object sender, EventArgs e)
        {
            if (this.currentSample != null)
            {
                this.RunCurrentSample();
            }
        }
    }
}