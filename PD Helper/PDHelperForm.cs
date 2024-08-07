using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text; 
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
//xbox input
using SharpDX.XInput;
/* ERROR CODES
* ERROR01: You didn't select a skill in your Arsenal.
* ERROR02: You didn't select a skill in the Arsenal Editor.
* ERROR03: You didn't select a skill in your Arsenal and in the Arsenal Editor.
* ERROR04: You didn't select an Arsenal in the Arsenal List.
* ERROR05: This Arsenal has skills from too many schools. You are limited to: X schools
* ERROR06: The loaded Arsenal loaded is not set to 1,2 or 3 Schools.
* ERROR07: The Profile has no Arsenals, please create at least one in-game Arsenal to be able to load/write/edit them.
* ERROR08: The arsenal name contains banned characters (\ / : * ? "" < > |)
* ERROR09: A Skill from your loaded arsenal does not exist in the game and could not be loaded. The arsenal has been tampered with or was corrupted. Please try loading another arsenal.
* ERROR10: The arsenal does not contain a valid amount of cards. The arsenal has been tampered with or is corrupted. Please try loading another arsenal.
* 
* 
* 
*/

namespace PD_Helper
{
    //timer value 7FF7D096C8C0
    public partial class PDHelperForm : Form
    {
        private Controller _controller;

        // Load card definitions
        PDArsenal editingArsenal;
        //string[] loadedDeck = { "FF FF", "FF FF", "FF FF", "FF FF", "FF FF", "FF FF", "FF FF", "FF FF", "FF FF", "FF FF", "FF FF", "FF FF", "FF FF", "FF FF", "FF FF", "FF FF", "FF FF", "FF FF", "FF FF", "FF FF", "FF FF", "FF FF", "FF FF", "FF FF", "FF FF", "FF FF", "FF FF", "FF FF", "FF FF", "FF FF", "00 00" };
        //string loadedDeckName = "";

        bool ProcOpen = false;
        bool gamepadOn = false;
        public PDMemory memory = new PDMemory();
        public ListBox allSkills = new ListBox();

        /// <summary>
        /// Initializes PDHelper.
        /// </summary>
        public PDHelperForm()
        {
            InitializeComponent();
            this.KeyPreview = true;

            // Set default checkmarks
            for (int i = 0; i < 5; i++)
            {
                schoolFilterCheckedListBox.SetItemChecked(i, true);
            }
            for (int i = 0; i < rangeFilterCheckedListBox.Items.Count; i++)
            {
                rangeFilterCheckedListBox.SetItemChecked(i, true);
            }
            for (int i = 0; i < 4; i++)
            {
                miscNumberCheckedListBox.SetItemChecked(i, true);
            }

            // Set default sort
            sortComboBox1.Text = "ID";
            sortComboBox2.Text = "None";
            sortComboBox3.Text = "None";

            // Fix the column size of deck listbox
            deckListBox.ColumnWidth = deckListBox.Width / 3 - 2;
            deckListBox.ItemHeight = deckListBox.Height / 10 - 1;
            editorList.ItemHeight = deckListBox.ItemHeight;

            // Load a blank 30 aura arsenal
            editingArsenal = new PDArsenal(string.Empty);
            openArsenalToList(editingArsenal);
            refreshView();
        }

        /// <summary>
        /// Lists all the saved arsenals in the folder, as well as loads all the skills into the editor.
        /// </summary>
        private void loadArsenalList()
        {
            // Load arsenal editor with skills from the database
            if (editorList.Items.Count == 0)
            {
                foreach (var item in PDCard.cardDef)
                {
                    editorList.Items.Add(item.Value.NAME);
                    allSkills.Items.Add(item.Value.NAME);
                }
            }

            // Add each arsenal file to the list
            savedArsenalListBox.Items.Clear();
            DirectoryInfo directory = new DirectoryInfo(@"Arsenals\"); // Assuming Arsenals is your Folder
            FileInfo[] Files = directory.GetFiles("*.arsenal"); // Getting arsenal files
            string str = "";

            foreach (FileInfo file in Files)
            {
                string currentDeck = file.Name;
                currentDeck = currentDeck.Remove(currentDeck.Length - 8);
                savedArsenalListBox.Items.Add(currentDeck);
            }
            arsenalListGroupBox.Text = "Arsenal List (" + savedArsenalListBox.Items.Count + ")";
        }

        /// <summary>
        /// Load the game data into PDHelper.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void loadGameData(object sender, EventArgs e)
        {
            arsenalDropdown.Items.Clear();

            //start input worker (if it hasn't yet)
            if (!gamepadOn)
            {
                _controller = new Controller(UserIndex.One);
                GamepadWorker.RunWorkerAsync();
                gamepadOn = true;
            }

            // Link to Phantom Dust
            if (memory.LinkPD() != null)
            {
                //Get Processes and check for Phantom Dust, attach and enable Arsenal Loading
                label2.Text = "Found the process! ID: " + memory.pdProcess.Id.ToString();
                label2.ForeColor = Color.Green;
                groupBox1.Enabled = true;

                //Read all names of Arsenals
                string[] arsenalNames = memory.GetArsenalNames();
				if (arsenalNames != null)
				{
					foreach (string arsenalName in arsenalNames)
					{
						if (arsenalName.Length > 0)
						{
                            arsenalDropdown.Items.Add(arsenalName);
						}
					}
				}

                if (arsenalDropdown.Items.Count > 0)
                {
                    arsenalDropdown.SelectedIndex = 0;
                }
                else
                {
                    MessageBox.Show("ERROR07: The Profile has no Arsenals, please create at least one in-game Arsenal to be able to load/write/edit them.");
                }
            }
            else
            {
                label2.ForeColor = Color.Red;
                label2.Text = "No Game Found. Start Phantom Dust First!";
            }
        }

        /// <summary>
        /// Grants max skills.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void giveMaxSkills(object sender, EventArgs e)
        {
            maxSkillsButton.Enabled = memory.GiveMaxSkills();
        }

        /// <summary>
        /// Grants max credits.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void giveMaxCredits(object sender, EventArgs e)
        {
			maxCreditsButton.Enabled = !memory.GiveMaxCredits();
        }

        /// <summary>
        /// Saves the editor arsenal to the game.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSaveToPDH_Click(object sender, EventArgs e)
        {
			// Only save if the name is given
			if (editingArsenal.Name == "")
			{
                MessageBox.Show("Please enter a name for the arsenal to save.");
                return;
			}
            
            string path = @"Arsenals\" + editingArsenal.Name + ".arsenal";
            string str = "";
            for (int i = 0; i < 30; i++)
            {
                str += editingArsenal[i].HEX + ",";
            }
            str += $"0{(int)schoolNumeric.Value} 00,";
            using (StreamWriter sw = File.CreateText(path))
            {
                sw.WriteLine(str);
                sw.Close();
            }
            // only add new name to list if its a unique new deck, update the old one otherwise
            if (!savedArsenalListBox.Items.Contains(editingArsenal.Name) == true) {
                savedArsenalListBox.Items.Add(editingArsenal.Name);
            }

            // Re-sort the arsenal
            List<PDCard> cardList = new List<PDCard>(editingArsenal.Cards);
            openArsenalToList(cardList, arsenalNameBox.Text, (int)schoolNumeric.Value);
        }

        /// <summary>
        /// Replace the selected skill with the one chosen in the editor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void replaceSkill(object sender, EventArgs e)
        {
            if (editorList.SelectedIndex != -1 && deckListBox.SelectedIndex != -1)
            {
                //set loaded deck card change
                PDCard card = PDCard.CardFromName(editorList.SelectedItem.ToString());

                string currentHex = card.HEX;
                editingArsenal[deckListBox.SelectedIndex] = card;
                Debug.WriteLine(card.HEX);

                //set loaded deck visual
                deckListBox.Items[deckListBox.SelectedIndex] = card.NAME;

                // Recount skills
                int auraCount = 0;
                foreach (var item in deckListBox.Items)
                {
                    if (item.ToString() == "Aura Particle") auraCount++;
                }
                skillCountLabel.Text = Convert.ToString(30 - auraCount) + "/30";
            }
            //error handling
            else if (editorList.SelectedIndex != -1) { MessageBox.Show("ERROR01: You didn't select a skill in your Arsenal."); }
            else if (deckListBox.SelectedIndex != -1) { MessageBox.Show("ERROR02: You didn't select a skill in the Arsenal Editor."); }
            else { MessageBox.Show("ERROR03: You didn't select a skill in your Arsenal and in the Arsenal Editor."); }
        }

        /// <summary>
        /// Resets the selected skill with Aura.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void resetSkill(object sender, EventArgs e)
        {
            if (deckListBox.SelectedIndex != -1)
            {
                //set loaded deck card change
                editingArsenal[deckListBox.SelectedIndex] = PDCard.cardDef["FF FF"];
                //set loaded deck visual
                deckListBox.Items[deckListBox.SelectedIndex] = "Aura Particle";

                // Recount skills
                int auraCount = 0;
                foreach (var item in deckListBox.Items)
                {
                    if (item.ToString() == "Aura Particle") auraCount++;
                }
                skillCountLabel.Text = Convert.ToString(30 - auraCount) + "/30";
            }
            //error handling
            else { MessageBox.Show("You didn't select a skill in your Arsenal."); }
        }

        /// <summary>
        /// Updates the arsenal name.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void arsenalNameBox_TextChanged(object sender, EventArgs e)
        {
            var textToWrite = arsenalNameBox.Text;
            var regex = new Regex(@"[\\\/\:\*\?\""\<\>\|]");
            if (!regex.IsMatch(textToWrite))
            {
                editingArsenal.Name = arsenalNameBox.Text;
            } else
            {
                arsenalNameBox.Text = arsenalNameBox.Text.Remove(arsenalNameBox.Text.Length - 1, 1);
                arsenalNameBox.SelectionStart = arsenalNameBox.TextLength;
            }
        }

        /// <summary>
        /// Refilters the arsenal editor based on when a control is edited.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void updateEditorList(object sender, EventArgs e)
        {
            // Use the current array of checkmarks
            bool[] schoolFilter = new bool[5];
            for (int i = 0; i < 5; i++)
            {
                schoolFilter[i] = schoolFilterCheckedListBox.GetItemChecked(i);
            }
            bool[] rangeFilter = new bool[rangeFilterCheckedListBox.Items.Count];
            for (int i = 0; i < 10; i++)
            {
                rangeFilter[i] = rangeFilterCheckedListBox.GetItemChecked(i);
            }
            bool[] miscNumberFilter = new bool[4];
            for (int i = 0; i < 4; i++)
            {
                miscNumberFilter[i] = miscNumberCheckedListBox.GetItemChecked(i);
            }

            updateEditorList(schoolFilter, rangeFilter, miscNumberFilter);
        }

        /// <summary>
        /// Refilters the arsenal editor based on the given filters.
        /// </summary>
        /// <param name="schoolFilter">Decides which schools to filter.</param>
        /// <param name="rangeFilter">Decides which ranges to filter.</param>
        /// <param name="miscNumberFilter">An extra parameter for the non-standard attributes.</param>
        /// <exception cref="FormatException"></exception>
        private void updateEditorList(bool[] schoolFilter, bool[] rangeFilter, bool[] miscNumberFilter)
        {
            // Step 0: Make a list of skills that contain all the ones to display
            List<PDCard> displayCards = new List<PDCard>();
            string infty = PDCard.CardFromName("Bomb").USAGE;
            foreach (var item in allSkills.Items)
            {
                // Step 1: Only consider the skills matching the school filter
                PDCard.School school = PDCard.SchoolFromName(item.ToString());
                bool toKeep = true;
                if (school != PDCard.School.Aura)
				{
                    toKeep = schoolFilter[(int)school];
                }
                /*
                switch (school)
                {
                    case "Psycho":
                        toKeep = schoolFilter[0];
                        break;
                    case "Optical":
                        toKeep = schoolFilter[1];
                        break;
                    case "Nature":
                        toKeep = schoolFilter[2];
                        break;
                    case "Ki":
                        toKeep = schoolFilter[3];
                        break;
                    case "Faith":
                        toKeep = schoolFilter[4];
                        break;
                    default:
                        break;
                }*/
                if (!toKeep) continue;

                // Step 2: Only consider the skills matching the range filter
                string range = PDCard.CardFromName(item.ToString()).RANGE;
                toKeep = true;
                switch (range)
                {
                    case "short":
                        toKeep = rangeFilter[0];
                        break;
                    case "medium":
                        toKeep = rangeFilter[1];
                        break;
                    case "long":
                        toKeep = rangeFilter[2];
                        break;
                    case "mine":
                        toKeep = rangeFilter[3];
                        break;
                    case "capsule":
                        toKeep = rangeFilter[4];
                        break;
                    case "-":
                        toKeep = rangeFilter[5];
                        break;
                    case "self":
                        toKeep = rangeFilter[6];
                        break;
                    case "all":
                        toKeep = rangeFilter[7];
                        break;
                    case "auto":
                        toKeep = rangeFilter[8];
                        break;
                    case "env":
                        toKeep = rangeFilter[9];
                        break;
                    default:
                        break;
                }
                if (!toKeep) continue;

                // Step 3: Now filter by type if necessary
                PDCard card = PDCard.CardFromName(item.ToString());
                if (!allSkillsRadioButton.Checked)
                {
                    // Get type
                    if (card == null)
                    {
                        throw new FormatException("Skill not found");
                    }
                    string type = card.TYPE;

                    // Match with the type marked
                    toKeep = false;
                    switch (type)
                    {
                        case "Attack":
                            toKeep = attackRadioButton.Checked;
                            break;
                        case "Defense":
                            toKeep = defenseRadioButton.Checked;
                            break;
                        case "Erase":
                            toKeep = eraseRadioButton.Checked;
                            break;
                        case "Status":
                            toKeep = statusRadioButton.Checked;
                            break;
                        case "Special":
                            toKeep = specialRadioButton.Checked;
                            break;
                        case "Environment":
                            toKeep = environmentalRadioButton.Checked;
                            break;
                    }
                    if (!toKeep) continue;
                }

                // Step 4: Filter by the STR/DEF/USE/COST given
                bool validSTR = (int.TryParse(card.DAMAGE, out int damage)
                        && damage >= (int)strMinNumeric.Value
                        && damage <= (int)strMaxNumeric.Value)
                    || card.TYPE != "Attack" || (miscNumberFilter[0] && card.DAMAGE == "X");
                bool validDEF = (int.TryParse(card.DAMAGE, out int defense)
                        && defense >= (int)defMinNumeric.Value
                        && defense <= (int)defMaxNumeric.Value)
                    || card.TYPE != "Defense" || (miscNumberFilter[1] && card.DAMAGE == "-");
                bool validUSE = (int.TryParse(card.USAGE, out int usage)
                        && usage >= (int)useMinNumeric.Value
                        && usage <= (int)useMaxNumeric.Value)
                    || (miscNumberFilter[2] && card.USAGE == infty);
                bool validCOST = (int.TryParse(card.COST, out int cost)
                        && cost >= (int)costMinNumeric.Value
                        && (cost <= (int)costMaxNumeric.Value
                            || costMaxNumeric.Value == costMaxNumeric.Maximum)) // Handle Phantom Dust Skill
                    || (miscNumberFilter[3] && card.COST == "X");
                toKeep = validSTR && validDEF && validUSE && validCOST;

                if (!toKeep) continue;

                // Step 5: search for value in editorList
                if (editorSearchTextBox.Text == "" || item.ToString().Contains(editorSearchTextBox.Text, StringComparison.OrdinalIgnoreCase))
                {
                    displayCards.Add(PDCard.CardFromName(item.ToString()));
                    //editorList.Items.Add(item.ToString());
                }
            }

            // Step 6: Determine the sorting method
            List<IComparer<PDCard>> comparers = new List<IComparer<PDCard>>();
            IComparer<PDCard> comparer1 = PDCard.DetermineSort(sortComboBox1.Text);
            if (comparer1 != null) comparers.Add(comparer1);
            else comparers.Add(PDCard.SortID()); // Default sort
            IComparer<PDCard> comparer2 = PDCard.DetermineSort(sortComboBox2.Text);
            if (comparer2 != null) comparers.Add(comparer2);
            IComparer<PDCard> comparer3 = PDCard.DetermineSort(sortComboBox3.Text);
            if (comparer3 != null) comparers.Add(comparer3);

            // Step 7: Sort the list and display it
            displayCards.Sort(PDCard.SortMulti(comparers.ToArray()));
            editorList.Items.Clear();
            foreach (var item in displayCards)
			{
                editorList.Items.Add(item.NAME);
            }
        }

        /// <summary>
        /// Loads an arsenal selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void loadArsenal(object sender, EventArgs e)
        {
            if (savedArsenalListBox.SelectedIndex != -1)
            {
                string name = savedArsenalListBox.SelectedItem.ToString();
                string path = @"Arsenals\" + name + ".arsenal";

				try
				{
                    editingArsenal = PDArsenal.LoadFromFile(name, path);
                }
				catch (Exception exception)
				{
                    MessageBox.Show(exception.Message,"Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
				}
                
                schoolNumeric.Value = Math.Max(1,editingArsenal.Schools.Count);
                deckListBox.Items.Clear();
                int auraCount = 0;
                for (int i = 0; i < 30; i++)
                {
					deckListBox.Items.Add(editingArsenal[i].NAME);
                    if (editingArsenal[i].TYPE == "Aura") auraCount++;
                }
                arsenalNameBox.Text = name;
                skillCountLabel.Text = Convert.ToString(30 - auraCount) + "/30";
            }
            else MessageBox.Show("ERROR04: You didn't select an Arsenal in the Arsenal List.");
        }

        /// <summary>
        /// Deletes the selected file after asking the user for confirmation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void deleteArsenal(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("Are you sure you want to delete Arsenal: " + savedArsenalListBox.SelectedItem.ToString() + "?", "Arsenal Deletion Check", MessageBoxButtons.YesNo);
            if (dr == DialogResult.Yes)
            {
                string path = @"Arsenals\" + savedArsenalListBox.SelectedItem.ToString() + ".arsenal";
                File.Delete(path);
                savedArsenalListBox.Items.Remove(savedArsenalListBox.SelectedItem);
            }
        }

        /// <summary>
        /// Changes the partner lock on check.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void partnerLock_CheckedChanged(object sender, EventArgs e)
        {
            memory.SetPartnerLockOn(partnerLock.Checked);
        }

        private Color lightColorFromType(string type) => ColorProfileForm.getColor(type, true);

        private Color lightColorFromName(string name) => lightColorFromType(PDCard.CardFromName(name).TYPE);

        private Color darkColorFromType(string type) => ColorProfileForm.getColor(type, false);

        private Color darkColorFromName(string name) => darkColorFromType(PDCard.CardFromName(name).TYPE);

        /// <summary>
        /// Displays the selected card info onto the editor.
        /// </summary>
        /// <param name="name"></param>
        private void displayEditorSkill(string name)
        {
            PDCard card = PDCard.CardFromName(name);

            labelSkillCost.Text = card.COST;
            labelSkillDescription.Text = card.DESCRIPTION;
            labelSkillID.Text = card.ID.ToString();
            labelSkillName.Text = card.NAME;
            labelSkilLRange.Text = card.RANGE;
            labelSkillSchool.Text = card.SCHOOL;
            labelSkillStrength.Text = card.DAMAGE;
            labelSkillUse.Text = card.USAGE;

            Color textColor = lightColorFromType(card.TYPE);
            labelSkillID.ForeColor = textColor;
            labelSkillName.ForeColor = textColor;
            labelSkillSchool.ForeColor = textColor;
        }

        /// <summary>
        /// Activates when a different card is selected in the editor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void editorList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (editorList.SelectedIndex != -1)
            {
                displayEditorSkill(editorList.SelectedItem.ToString());
            }
        }

        /// <summary>
        /// Determines whether the current arsenal is valid for use.
        /// </summary>
        /// <returns></returns>
        private bool validateArsenal()
        {
            //school limit checking
            int psy = 0;
            int opt = 0;
            int nat = 0;
            int ki = 0;
            int fai = 0;
            int schoolAmount = 0;
            PDCard currentCard = new PDCard();
            foreach (var cardName in deckListBox.Items)
            {
                currentCard = PDCard.CardFromName(cardName.ToString());
                switch (currentCard.SCHOOL)
                {
                    case "Psycho":
                        psy++;
                        break;
                    case "Optical":
                        opt++;
                        break;
                    case "Nature":
                        nat++;
                        break;
                    case "Ki":
                        ki++;
                        break;
                    case "Faith":
                        fai++;
                        break;
                    case "Aura":
                        break;
                }
            }
            int maxAllowedSchools = (int)schoolNumeric.Value;
            if (psy > 0) { schoolAmount++; }
            if (opt > 0) { schoolAmount++; }
            if (nat > 0) { schoolAmount++; }
            if (ki > 0) { schoolAmount++; }
            if (fai > 0) { schoolAmount++; }

            //dupe limit checking
            bool isOverDupeLimit = false;
            Dictionary<string, int> skillDupes = new Dictionary<string, int>();
            for (int i = 0; i < 30; i++)
            {
                if (skillDupes.ContainsKey(deckListBox.Items[i].ToString()))
                {
                    skillDupes[deckListBox.Items[i].ToString()]++;
                }
                else
                {
                    skillDupes.Add(deckListBox.Items[i].ToString(), 1);
                }
            }
            foreach (var item in skillDupes)
            {
                if (item.Value > 3 && item.Key != "Aura Particle")
                {
                    isOverDupeLimit = true;
                }
            }

            if (schoolAmount > maxAllowedSchools)
            {
                MessageBox.Show("ERROR05: This Arsenal has skills from too many schools. You are limited to: " + maxAllowedSchools.ToString() + " School(s)");
                return false;
            }
            else if (isOverDupeLimit)
            {
                MessageBox.Show("ERROR06: You cannot have more than 3 of the same skill in an Arsenal");
                return false;
            }
            else return true;
        }

        /// <summary>
        /// Saves the arsenal to the game if it is valid.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveToPDbtn_Click(object sender, EventArgs e)
        {
			try
			{
                if (validateArsenal())
                {
                    editingArsenal.Name = arsenalNameBox.Text;
                    memory.SetArsenal(arsenalDropdown.SelectedIndex, editingArsenal, (int)schoolNumeric.Value);
                    arsenalDropdown.Items[arsenalDropdown.SelectedIndex] = arsenalNameBox.Text.ToString();
                }
            }
			catch (Exception)
			{
                MessageBox.Show("Unable to save to Phantom Dust. Make sure the game is open and the app is connected by pressing 'Load Profile'.");
			}
        }

        /// <summary>
        /// Opens the folder containing the arsenal files.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void openArsenalFolder(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", @"Arsenals\");
        }
        
        /// <summary>
        /// Places the given cards, with associated name and school amount, onto the listing.
        /// </summary>
        /// <param name="cardList">The cards to be edited into the arsenal.</param>
        /// <param name="arsenalName">The arsenal name.</param>
        /// <param name="schoolAmount">The number of schools in the arsenal.</param>
        private void openArsenalToList(List<PDCard> cardList, string arsenalName = "", int schoolAmount = 2)
        {
            // Sort the list
            cardList.Sort(PDCard.SortType());

            // Set arsenal
            editingArsenal = new PDArsenal(arsenalName, cardList.ToArray());

            // Enter card to the list box. Also count aura
            int auraCount = 0;
            deckListBox.Items.Clear();
            for (int i = 0; i < 30; i++)
            {
                deckListBox.Items.Add(cardList[i].NAME);
                if (cardList[i].TYPE == "Aura") auraCount++;
            }

            // Set the skill count, school amount, and arsenal name
            skillCountLabel.Text = Convert.ToString(30 - auraCount) + "/30";
            schoolNumeric.Value = schoolAmount;
            arsenalNameBox.Text = arsenalName;
            editingArsenal.Name = arsenalName;

            loadArsenalList();
        }

        /// <summary>
        /// Places the arsenal into the listing.
        /// </summary>
        /// <param name="arsenal">The arsenal to be edited.</param>
        private void openArsenalToList(PDArsenal arsenal)
        {
            // Sort the list
            List<PDCard> cardList = new List<PDCard>(arsenal.Cards);
            cardList.Sort(PDCard.SortType());

            // Enter card to the list box. Also count aura
            int auraCount = 0;
            deckListBox.Items.Clear();
            for (int i = 0; i < 30; i++)
            {
                deckListBox.Items.Add(cardList[i].NAME);
                if (cardList[i].TYPE == "Aura") auraCount++;
            }

            // Set the skill count, school amount, and arsenal name
            skillCountLabel.Text = Convert.ToString(30 - auraCount) + "/30";
            schoolNumeric.Value = Math.Max(1,arsenal.Schools.Count);
            arsenalNameBox.Text = arsenal.Name;

            loadArsenalList();
        }

        /// <summary>
        /// Loads a new PD arsenal from the game into the editor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void arsenalDropdown_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (memory.GetPartnerLockOn())
            {
                partnerLock.Checked = true;
            }

            //System.Diagnostics.Debug.WriteLine(o1);
            
            // Load all cards
            Byte[] loadDeck = memory.GetArsenalCardsBytes(arsenalDropdown.SelectedIndex);
            
            //add cards to list
            List<PDCard> cardList = new List<PDCard>();
            int o = 0;
            for (int i = 0; i < 30; i++)
            {
                Byte[] currentByte = { loadDeck[o], loadDeck[o + 1] };
                String currentHexString = BitConverter.ToString(currentByte).Replace('-', ' ');
                cardList.Add(PDCard.cardDef[currentHexString]);
                o += 2;
            }

            //manual write school amount
            Byte[] currentByteFix = { loadDeck[60], loadDeck[61] };
            String currentHexStringFix = BitConverter.ToString(currentByteFix).Replace('-', ' ');
            //loadedDeck[30] = currentHexStringFix;
            string loadSchoolAmount = currentHexStringFix.Remove(currentHexStringFix.Length - 3);
            openArsenalToList(cardList, arsenalDropdown.SelectedItem.ToString(), Int32.Parse(loadSchoolAmount)); 
        }

        /// <summary>
        /// Displays the newly selected card in the arsenal onto the editor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void deckListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Make the editor select the new skill
            if (deckListBox.SelectedIndex != -1)
            {
                displayEditorSkill(deckListBox.SelectedItem.ToString());
            }
        }

        /// <summary>
        /// Draws the card in the ListBox with its associated type color and school icon.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void skillList_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            // Get the list box
            ListBox listBox = (ListBox)sender;

            // Set back color
            string skillName = listBox.Items[e.Index].ToString();
            Color backColor = (e.State & DrawItemState.Selected) == DrawItemState.Selected ? lightColorFromName(skillName) : darkColorFromName(skillName);

            // Color rectangle
            e.Graphics.FillRectangle(new SolidBrush(backColor), e.Bounds);

            // Draw the current item text
            e.Graphics.DrawString(skillName, e.Font, new SolidBrush(Color.Black), e.Bounds, StringFormat.GenericDefault);

            // Get the school of the skill and its associate image path
            string schoolName = Enum.GetName(PDCard.SchoolFromName(skillName));
            string path = @"School_Icons\" + schoolName + ".png";
            Image schoolIcon = Image.FromFile(path);

            // Get size ratio
            float ratio = (float)e.Bounds.Height / (float)schoolIcon.Height;

            // Draw the school icon [HEIGHT IS 15]
            e.Graphics.DrawImage(schoolIcon,
                x: (float)e.Bounds.Right - ratio * (float)schoolIcon.Width,
                y: (float)e.Bounds.Top,
                width: ratio * (float)schoolIcon.Width,
                height: (float)e.Bounds.Height
                );
        }

        /// <summary>
        /// Unused; Toggles partner lock on based on the keyboard.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        { 
            // this should be the keypress T to toggle partner lock but its not working while not focused
            //if (e.KeyData == Keys.T) { partnerLock.Invoke((MethodInvoker)(() => partnerLock.Checked = !partnerLock.Checked)); }
        }

        /// <summary>
        /// Refreshes the current arsenal files displayed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void refreshArsenalList(object sender, EventArgs e)
        {
            //add each arsenal file to the list
            savedArsenalListBox.Items.Clear();
            DirectoryInfo directory = new DirectoryInfo(@"Arsenals\"); //Assuming Test is your Folder

            FileInfo[] Files = directory.GetFiles("*.arsenal"); //Getting Text files
            string str = "";

            foreach (FileInfo file in Files)
            {
                string currentDeck = file.Name;
                currentDeck = currentDeck.Remove(currentDeck.Length - 8);
                savedArsenalListBox.Items.Add(currentDeck);
            }
            arsenalListGroupBox.Text = "Arsenal List (" + savedArsenalListBox.Items.Count + ")";
        }

        /// <summary>
        /// Toggles the partner lock on based on the controller.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GamepadWorker_DoWork_1(object sender, DoWorkEventArgs e)
        {
            // previous state tracking
            string previousState = "null";
            while (_controller.IsConnected)
            {
                //making sure previous state is not same to prevent flicker
                if (_controller.GetState().Gamepad.Buttons.ToString() == "RightThumb" & previousState != "RightThumb")
                {
                    partnerLock.Invoke((MethodInvoker)(() => partnerLock.Checked = !partnerLock.Checked));
                }
                previousState = _controller.GetState().Gamepad.Buttons.ToString();
                //System.Diagnostics.Debug.WriteLine(_controller.GetState().Gamepad);
            }
        }

        /// <summary>
        /// Modify the school filter.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void schoolFilterCheckedListBox_ItemCheck(object sender, ItemCheckEventArgs e)
		{
            // Use the current array of checkmarks but with the updated checkmark value instead
            bool[] schoolFilter = new bool[5];
            for (int i = 0; i < 5; i++)
            {
                if (i != e.Index) schoolFilter[i] = schoolFilterCheckedListBox.GetItemChecked(i);
                else schoolFilter[e.Index] = e.NewValue == CheckState.Checked;
            }
            bool[] rangeFilter = new bool[rangeFilterCheckedListBox.Items.Count];
            for (int i = 0; i < rangeFilterCheckedListBox.Items.Count; i++)
            {
                rangeFilter[i] = rangeFilterCheckedListBox.GetItemChecked(i);
            }
            bool[] miscNumberFilter = new bool[4];
            for (int i = 0; i < 4; i++)
            {
                miscNumberFilter[i] = miscNumberCheckedListBox.GetItemChecked(i);
            }

            updateEditorList(schoolFilter, rangeFilter, miscNumberFilter);
        }

        /// <summary>
        /// Modify the range filter.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rangeFilterCheckedListBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            // Use the current array of checkmarks but with the updated checkmark value instead
            bool[] schoolFilter = new bool[5];
            for (int i = 0; i < 5; i++)
            {
                schoolFilter[i] = schoolFilterCheckedListBox.GetItemChecked(i);
            }
            bool[] rangeFilter = new bool[rangeFilterCheckedListBox.Items.Count];
            for (int i = 0; i < rangeFilterCheckedListBox.Items.Count; i++)
            {
                if (i != e.Index) rangeFilter[i] = rangeFilterCheckedListBox.GetItemChecked(i);
                else rangeFilter[e.Index] = e.NewValue == CheckState.Checked;
            }
            bool[] miscNumberFilter = new bool[4];
            for (int i = 0; i < 4; i++)
            {
                miscNumberFilter[i] = miscNumberCheckedListBox.GetItemChecked(i);
            }

            updateEditorList(schoolFilter, rangeFilter, miscNumberFilter);
        }

        /// <summary>
        /// Modify the misc filter.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void miscNumberCheckedListBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            // Use the current array of checkmarks but with the updated checkmark value instead
            bool[] schoolFilter = new bool[5];
            for (int i = 0; i < 5; i++)
            {
                schoolFilter[i] = schoolFilterCheckedListBox.GetItemChecked(i);
            }
            bool[] rangeFilter = new bool[rangeFilterCheckedListBox.Items.Count];
            for (int i = 0; i < rangeFilterCheckedListBox.Items.Count; i++)
            {
                rangeFilter[i] = rangeFilterCheckedListBox.GetItemChecked(i);
            }
            bool[] miscNumberFilter = new bool[4];
			for (int i = 0; i < 4; i++)
			{
                if (i != e.Index) miscNumberFilter[i] = rangeFilterCheckedListBox.GetItemChecked(i);
                else miscNumberFilter[e.Index] = e.NewValue == CheckState.Checked;
            }

            updateEditorList(schoolFilter, rangeFilter, miscNumberFilter);
        }

        /// <summary>
        /// Open the color profile menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void colorProfileButton_Click(object sender, EventArgs e)
		{
            // Open new form for setting colors
            var colorForm = new ColorProfileForm();
            colorForm.Owner = this;
            colorForm.ShowDialog();
            refreshView();
		}

        /// <summary>
        /// Refreshes the application. Used when new colors are loaded.
        /// </summary>
        public void refreshView()
        {
            // Refresh the two editor views
            deckListBox.Refresh();
            editorList.Refresh();

            // Set the color on the radio button text
            attackRadioButton.ForeColor = lightColorFromType("Attack");
            attackRadioButton.Refresh();
            defenseRadioButton.ForeColor = lightColorFromType("Defense");
            defenseRadioButton.Refresh();
            eraseRadioButton.ForeColor = lightColorFromType("Erase");
            eraseRadioButton.Refresh();
            environmentalRadioButton.ForeColor = lightColorFromType("Environment");
            environmentalRadioButton.Refresh();
            statusRadioButton.ForeColor = lightColorFromType("Status");
            statusRadioButton.Refresh();
            specialRadioButton.ForeColor = lightColorFromType("Special");
        }

        /// <summary>
        /// Removes the selection when checking a checkmark.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void checkedListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
            ((CheckedListBox)sender).ClearSelected();
		}

        /// <summary>
        /// Load a blank 30 aura arsenal.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void newArsenalButton_Click(object sender, EventArgs e)
		{
            editingArsenal = new PDArsenal(string.Empty);
            openArsenalToList(editingArsenal);
        }

        /// <summary>
        /// Draws the arsenal file in the listing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void savedArsenalListBox_DrawItem(object sender, DrawItemEventArgs e)
		{
            // TODO: Ensure elements scale properly
            
            // Get ListBox
            ListBox listBox = (ListBox)sender;

            // Background
            e.DrawBackground();

            // Text
            string name = listBox.Items[e.Index].ToString();
            e.Graphics.DrawString(name, e.Font, new SolidBrush(e.ForeColor), e.Bounds, StringFormat.GenericDefault);

            // Focus
            e.DrawFocusRectangle();

            // Load the arsenal from the file
            string path = @"Arsenals\" + name + ".arsenal";
            PDArsenal arsenal = PDArsenal.LoadFromFile(name, path);

            // Get the schools, and reverse the order
            List<PDCard.School> schools = arsenal.Schools;
            schools.Reverse();

			// Add school images.
			for (int i = 0; i < schools.Count; i++)
			{
                // Get the school icon as bitmap
                string schoolName = Enum.GetName(schools[i]);
                string iconPath = @"School_Icons\" + schoolName + ".png";
                Bitmap schoolIcon = (Bitmap)Image.FromFile(iconPath);

				// Change the color of the icons
				for (int y = 0; y < schoolIcon.Height; y++)
				{
					for (int x = 0; x < schoolIcon.Width; x++)
					{
						if (schoolIcon.GetPixel(x,y).A != 0)
						{
                            schoolIcon.SetPixel(x, y, e.ForeColor);
                        }
					}
				}

                // Get size ratio
                float ratio = (float)e.Bounds.Height / (float)schoolIcon.Height;

                // Draw the school icon
                e.Graphics.DrawImage(schoolIcon,
                    x: (float)e.Bounds.Right - ratio * (float)schoolIcon.Width * (i + 1),
                    y: (float)e.Bounds.Top,
                    width: ratio * (float)schoolIcon.Width,
                    height: (float)e.Bounds.Height
                    );
            }
		}
	}
}