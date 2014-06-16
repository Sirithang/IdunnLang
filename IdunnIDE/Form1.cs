using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using IdunnParser;


namespace IdunnIDE
{
    public partial class Form1 : Form
    {
        Parser p;

        public Form1()
        {
            InitializeComponent();

            p = new Parser();
            p.logFunc = LogToLabel;

            p.AddArchetype("{ ship : { name : \"default\", crew : [], cargo : [] } }");
        }


        public void LogToLabel(string s)
        {
            this.label1.Text += s + "\n";
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            System.IO.StreamReader r = new System.IO.StreamReader("../../test/ship.idunn");

            p.Parse(r.ReadToEnd());

            r.Close();


            TreeNode n = new TreeNode("root");
            RecursiveTree(n, p._root);
            this.treeView1.Nodes.Add(n);
        }

        private void RecursiveTree(TreeNode currentRoot, SimpleJSON.JSONClass current)
        {
            foreach(KeyValuePair<string, SimpleJSON.JSONNode> N in current)
            {

                TreeNode node = new TreeNode(N.Key);

                if (N.Value.AsObject != null)
                    RecursiveTree(node, N.Value.AsObject);
                else
                    node.Text = N.Key + " : " + N.Value;

                currentRoot.Nodes.Add(node);
            }
        }

    }
}
