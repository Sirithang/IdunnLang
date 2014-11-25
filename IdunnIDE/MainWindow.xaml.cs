using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SimpleJSON;

namespace IdunnIDE
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string databaseFile;

        public IdunnParser.Parser parser = new IdunnParser.Parser();

        public void logger(string str)
        {
            System.Console.Out.Write(str + "\n");
        }

        public MainWindow()
        {
            parser.logFunc = logger;
            InitializeComponent();


        }



        public void RefereshArchetypes()
        {
            int deleteFrom = -1;

            for (int i = 0; i < this.archetypeListview.Items.Count; ++i)
            {//go throught existing items to check if one have changed
                if (i >= parser.database["archetypes"].Count)
                {//we have more item than archetype in the database, delete them
                    deleteFrom = i;
                }
                else
                {
                    if (((ListViewItem)this.archetypeListview.Items[i]).Content != parser.database["archetypes"][i]["archetype"].Value)
                    {
                        ((ListViewItem)this.archetypeListview.Items[i]).Content = parser.database["archetypes"][i]["archetype"].Value;
                    }
                }
            }

            if (deleteFrom >= 0)
            {
                while (this.archetypeListview.Items.Count > parser.database["archetypes"].Count)
                    this.archetypeListview.Items.RemoveAt(this.archetypeListview.Items.Count - 1);
            }

            for (int i = this.archetypeListview.Items.Count; i < parser.database["archetypes"].Count; ++i)
            {
                JSONClass c = parser.database["archetypes"][i].AsObject;
                if (c.Contains("archetype"))
                {
                    ListViewItem item = new ListViewItem();
                    item.Content = c["archetype"].Value;
                    this.archetypeListview.Items.Add(item);
                }
            }
        }

        public void AddNewArchetype()
        {
            if(parser.database == null)
                return;

            string name = "archetype_";
            int freeIdx = -1;
            bool isFree = false;

            while (!isFree)
            {
                isFree = true;
                freeIdx += 1;

                for (int i = 0; i < parser.database["archetypes"].Count; ++i)
                {
                    if (parser.database["archetypes"][i]["archetype"].Value == name + freeIdx)
                    {
                        isFree = false;
                        break;
                    }
                }
            }

            JSONClass c = new JSONClass();
            c.Add("archetype", name + freeIdx);

            parser.database["archetypes"].Add(c);

            RefereshArchetypes();
        }


        protected JSONClass _currentSelected = null;
        public void RefreshEventList()
        {
            selectInhibited = true;
            this.eventTreeView.Items.Clear();

            for (int i = this.eventTreeView.Items.Count; i < parser.database["events"].Count; ++i)
            {
                JSONClass c = parser.database["events"][i].AsObject;

                TreeViewItem viewItem = new TreeViewItem();
                this.eventTreeView.Items.Add(viewItem);

                if (RecursiveChildAdd(viewItem, c))
                {
                    viewItem.ExpandSubtree();
                }
            }
        }

        protected bool RecursiveChildAdd(TreeViewItem parent, JSONClass c)
        {
            bool needExpansion = false;

            parent.Header = c["name"].Value;
            parent.Tag = c;

            if (c == _currentSelected)
            {
                needExpansion = true;
                parent.IsSelected = true;
            }

            foreach (JSONClass child in c["childs"].AsArray)
            {
                TreeViewItem item = new TreeViewItem();

                needExpansion |= RecursiveChildAdd(item, child);

                parent.Items.Add(item);
            }

            return needExpansion;
        }

        public JSONClass AddNewEvent(JSONClass parent = null)
        {
            if (parser.database == null)
                return null;

            string name = "event_";
            int freeIdx = -1;
            bool isFree = false;

            JSONArray target;

            if (parent != null)
            {
                target = parent["childs"].AsArray;
            }
            else
            {
                target = parser.database["events"].AsArray;
            }

            while (!isFree)
            {
                isFree = true;
                freeIdx += 1;

                for (int i = 0; i < target.Count; ++i)
                {
                    if (target[i]["name"].Value == name + freeIdx)
                    {
                        isFree = false;
                        break;
                    }
                }
            }

            JSONClass c = new JSONClass();
            c.Add("name", name + freeIdx);
            c.Add("text", "ENTER TEXT HERE");
            c.Add("condition", "");
            c.Add("exec", "");
            c.Add("invalid", "");
            c.Add("valid", "");
            c.Add("childs", new JSONArray());

            target.Add(c);

            RefreshEventList();

            return c;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();

            // Set filter for file extension and default file extension
            dlg.DefaultExt = ".idb";
            dlg.Filter = "Idunn database (.idb)|*.idb";

            // Display OpenFileDialog by calling ShowDialog method
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox
            if (result == true)
            {
                // Open document
                databaseFile = dlg.FileName;
                parser.database = JSON.Parse("{}");
                parser.database["archetypes"] = new JSONArray();
                RefereshArchetypes();
            }
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            System.IO.File.WriteAllText(databaseFile, IdunnParser.Helpers.PrettyFormat(parser.database.ToString()));
        }

        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension
            dlg.DefaultExt = ".idb";
            dlg.Filter = "Idunn database (.idb)|*.idb";

            // Display OpenFileDialog by calling ShowDialog method
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox
            if (result == true)
            {
                try
                {
                    JSONNode res = JSON.Parse(System.IO.File.ReadAllText(dlg.FileName));
                    if (res != null)
                    {
                        parser.database = res;
                        databaseFile = dlg.FileName;
                        RefereshArchetypes();
                        RefreshEventList();
                    }
                }
                catch (System.Exception ex)
                {

                }
            }
        }

        private void button1_Click_1(object sender, RoutedEventArgs e)
        {
            AddNewArchetype();
        }

        private void archetypeListview_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.archetypeListview.SelectedIndex < 0)
                return;

            JSONClass c = parser.database["archetypes"][this.archetypeListview.SelectedIndex].AsObject;

            if (c != null)
            {
                this.archetypeCodeEditor.Text = IdunnParser.Helpers.PrettyFormat(c.ToString());
            }
        }

        private void archetypeCodeEditor_TextChanged(object sender, EventArgs e)
        {
            try
            {
                JSONClass c = JSON.Parse(this.archetypeCodeEditor.Text).AsObject;

                if (c != null)
                {
                    bool needrefresh = false;
                    if(parser.database["archetypes"][this.archetypeListview.SelectedIndex]["archetype"].Value != c["archetype"].Value)
                    {
                        needrefresh = true;
                    }

                    parser.database["archetypes"][this.archetypeListview.SelectedIndex] = c;
                    
                    if(needrefresh)
                        RefereshArchetypes();
                }
            }
            catch (System.Exception ex)
            {
 
            }
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            AddNewEvent();
        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            if (eventTreeView.SelectedItem == null)
                return;

            TreeViewItem item = eventTreeView.SelectedItem as TreeViewItem;

            JSONClass c = item.Tag as JSONClass;

            AddNewEvent(c);
        }


        //-------------------

        private void eventExec_TextChanged(object sender, EventArgs e)
        {
            if (eventTreeView.SelectedItem == null)
                return;

            TreeViewItem item = eventTreeView.SelectedItem as TreeViewItem;

            JSONClass c = item.Tag as JSONClass;

            c["exec"] = this.eventExec.Text;

            //UpdateData(item);
        }

        private void eventConditionCodeEditor_TextChanged(object sender, EventArgs e)
        {
            if (eventTreeView.SelectedItem == null)
                return;

            TreeViewItem item = eventTreeView.SelectedItem as TreeViewItem;

            JSONClass c = item.Tag as JSONClass;

            c["condition"] = this.eventCondition.Text;
            //UpdateData(item);

        }

        private void textBox3_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (eventTreeView.SelectedItem == null)
                return;

            TreeViewItem item = eventTreeView.SelectedItem as TreeViewItem;

            JSONClass c = item.Tag as JSONClass;

            c["text"] = this.textBox3.Text;

            //UpdateData(item);
        }

        protected bool selectInhibited = false;

        private void eventTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (selectInhibited)
            {
                selectInhibited = false;
                return;
            }
            TreeViewItem item = (TreeViewItem)e.NewValue;

            if (item != null && item.Tag == _currentSelected)
                return;

            UpdateData(item);
        }

        protected void UpdateData(TreeViewItem item)
        {
            if (item != null && item.Tag != null)
            {
                JSONClass c = item.Tag as JSONClass;
                _currentSelected = c;

                this.eventValidText.Text = c["valid"].Value;
                this.eventInvalidText.Text = c["invalid"].Value;

                this.eventExec.Text = c["exec"].Value;
                this.eventCondition.Text = c["condition"].Value;
                this.textBox3.Text = c["text"].Value;

                this.eventNameBox.Text = c["name"].Value;
                this.eventTagBox.Text = c["tags"].Value;
            }
            else
            {
                this.eventValidText.Text = "";
                this.eventInvalidText.Text = "";

                this.eventExec.Text = "";
                this.eventCondition.Text = "";
                this.textBox3.Text = "";

                this.eventNameBox.Text = "";
                this.eventTagBox.Text = "";

                _currentSelected = null;
            }
        }

        private void eventNameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (eventTreeView.SelectedItem == null)
                return;

            TreeViewItem item = eventTreeView.SelectedItem as TreeViewItem;

            JSONClass c = item.Tag as JSONClass;

            c["name"] = this.eventNameBox.Text;

            if(c["name"].Value != item.Header)
                RefreshEventList();
        }

        private void eventTagBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (eventTreeView.SelectedItem == null)
                return;

            TreeViewItem item = eventTreeView.SelectedItem as TreeViewItem;

            JSONClass c = item.Tag as JSONClass;

            c["tags"] = this.eventTagBox.Text;

            //UpdateData(item);
        }

        private void eventValidText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (eventTreeView.SelectedItem == null)
                return;

            TreeViewItem item = eventTreeView.SelectedItem as TreeViewItem;

            JSONClass c = item.Tag as JSONClass;

            c["valid"].Value = this.eventValidText.Text;
        }

        private void eventInvalidText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (eventTreeView.SelectedItem == null)
                return;

            TreeViewItem item = eventTreeView.SelectedItem as TreeViewItem;

            JSONClass c = item.Tag as JSONClass;

            c["invalid"].Value = this.eventInvalidText.Text;
        }
    }
}
