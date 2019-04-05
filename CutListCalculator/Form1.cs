using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CutListCalculator
{
    public partial class Form1 : Form
    {

        public class BoardCost
        {
            int length;
            double cost;

            public BoardCost(int lengthFeet, double costDollars)
            {
                this.length = lengthFeet;
                this.cost = costDollars;
            }

            public double GetCost()
            {
                return cost;
            }

            public int GetFeet()
            {
                return length;
            }

            public int GetInches()
            {
                return length * 12;
            }
        }

        public class BoardCuts : BoardCost
        {
            List<double> cutList;

            public BoardCuts(BoardCost typeCost, List<double> cutList) :
                base(typeCost.GetFeet(), typeCost.GetCost())
            {
                this.cutList = cutList;
            }

            public List<double> GetCuts()
            {
                return cutList;
            }
        }

        public class ShoppingList
        {
            List<BoardCuts> boards;

            public ShoppingList()
            {
                this.boards = new List<BoardCuts>();
            }

            public ShoppingList(List<BoardCuts> boards)
            {
                this.boards = boards;
            }

            public void Add(BoardCuts boardCuts)
            {
                boards.Add(boardCuts);
            }

            public bool Empty()
            {
                return !boards.Any();
            }

            public double GetCost()
            {
                double cost = 0;
                foreach (BoardCuts item in boards)
                {
                    cost += item.GetCost();
                }
                return cost;
            }

            public List<BoardCuts> GetBoards()
            {
                return boards;
            }
        }

        List<List<BoardCost>> g_brdFootCost = new List<List<BoardCost>>()
        {
            new List<BoardCost>(){ new BoardCost(4, 1.95), new BoardCost(6, 2.80), new BoardCost(8, 2.22), new BoardCost(10, 4.62), new BoardCost(12, 5.51) }, //ONE_BY_TWO
            new List<BoardCost>(){ new BoardCost(4, 3.11), new BoardCost(6, 4.14), new BoardCost(8, 2.63), new BoardCost(10, 6.84), new BoardCost(12, 8.27) },  //ONE BY THREE
            new List<BoardCost>(){ new BoardCost(6, 3.28), new BoardCost(8, 4.38), new BoardCost(10, 5.46), new BoardCost(12, 6.55) }, //ONE BY FOUR
            new List<BoardCost>(){ new BoardCost(6, 4.80), new BoardCost(8, 6.36), new BoardCost(10, 7.97), new BoardCost(12, 9.57) }, //ONE BY SIX
            new List<BoardCost>(){},
            new List<BoardCost>(){},
            new List<BoardCost>(){},
            new List<BoardCost>(){ new BoardCost(7, 2.38), new BoardCost(8, 2.66), new BoardCost(10, 4.17), new BoardCost(12, 5.15)}, //TWO BY FOUR
            new List<BoardCost>(){ new BoardCost(3, 2.29), new BoardCost(8, 4.96), new BoardCost(10, 5.97), new BoardCost(12, 6.65), new BoardCost(14, 7.73), new BoardCost(16, 8.92)}, //TWO BY SIX
            new List<BoardCost>(){},
            new List<BoardCost>(){ new BoardCost(3, 3.95), new BoardCost(4, 4.26), new BoardCost(6, 5.09), new BoardCost(8, 6.98), new BoardCost(10, 9.42), new BoardCost(12, 10.38), new BoardCost(14, 12.54), new BoardCost(16, 13.96)}, //TWO_BY_TEN
            new List<BoardCost>(){ new BoardCost(8, 8.00)}   //FOUR BY FOUR
        };


        List<List<double>> lengthsNeeded = new List<List<double>>((int)(boardType.FOUR_BY_FOUR) + 1);

        string g_currentFileName = "";

        public static T GetValueFromDescription<T>(string description)
        {
            var type = typeof(T);
            if (!type.IsEnum) throw new InvalidOperationException();
            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attribute != null)
                {
                    if (attribute.Description == description)
                        return (T)field.GetValue(null);
                }
                else
                {
                    if (field.Name == description)
                        return (T)field.GetValue(null);
                }
            }
            throw new ArgumentException("Not found.", "description");
            // or return default(T);
        }


        public Form1()
        {
            InitializeComponent();

            boardTypeBox.DisplayMember = "Description";
            boardTypeBox.ValueMember = "Value";
            boardTypeBox.DataSource = Enum.GetValues(typeof(boardType))
                .Cast<Enum>()
                .Select(value => new
                {
                    (Attribute.GetCustomAttribute(value.GetType().GetField(value.ToString()), typeof(DescriptionAttribute)) as DescriptionAttribute).Description,
                    value
                })
                .OrderBy(item => item.value)
                .ToList();

            for (int i = 0; i < (int)(boardType.FOUR_BY_FOUR) + 1; i++)
            {
                lengthsNeeded.Add(new List<double>(0));
            }

            //Presort the board foot cost array, longest to shortest;
            for (int i = 0; i < g_brdFootCost.Count; i++)
            {
                g_brdFootCost[i].Sort((x, y) => y.GetInches().CompareTo(x.GetInches()));
            }
        }

        public static string GetEnumDescription(Enum value)
        {
            var fi = value.GetType().GetField(value.ToString());

            DescriptionAttribute[] attributes =
                (DescriptionAttribute[])fi.GetCustomAttributes(
                typeof(DescriptionAttribute),
                false);

            if (attributes != null &&
                attributes.Length > 0)
                return attributes[0].Description;
            else
                return value.ToString();
        }

        private void UpdateLengthsBox()
        {
            LengthsDisplay.Text = GetSaveFormat();
        }

        private void AddLengthsButton_Click(object sender, EventArgs e)
        {
            double valueToAdd;
            if (!Double.TryParse(neededLengthsBox.Text, out valueToAdd))
                MessageBox.Show("Length must be numeric.", "Parsing error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
                lengthsNeeded[boardTypeBox.SelectedIndex].Add(valueToAdd);

            UpdateLengthsBox();
        }

        double FindMostEfficientUse(ref List<double> solutionList, ref List<double> lengthsNeeded, double remainingLength, int startItr)
        {
            if (startItr >= lengthsNeeded.Count || lengthsNeeded[startItr] > remainingLength)
            {
                return remainingLength;
            }
            else
            {
                solutionList.Add(lengthsNeeded[startItr]);
                remainingLength -= lengthsNeeded[startItr];
                double bestCost = remainingLength;
                List<double> bestSolutionList = new List<double>();
                for (int i = startItr + 1; i < lengthsNeeded.Count; i++)
                {
                    List<double> subSolutionList = new List<double>();
                    double cost = 0;
                    if ((cost = FindMostEfficientUse(ref subSolutionList, ref lengthsNeeded, remainingLength, i)) < bestCost)
                    {
                        bestCost = cost;
                        bestSolutionList = subSolutionList;
                    }
                }
                solutionList.AddRange(bestSolutionList);
                return bestCost;
            }
        }

        private void ComputeList_Click(object sender, EventArgs e)
        {
            CalculateResults(false);
        }

        private void ComputeCutList_Click(object sender, EventArgs e)
        {
            CalculateResults(true);
        }

        private void CalculateResults(bool doCutList)
        {
            double g_maxTransportableInches;
            if (!Double.TryParse(maxTransportBox.Text, out g_maxTransportableInches))
            {
                MessageBox.Show("Max inches must be numeric.", "Parsing error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            //Clone the permanent list, and then:
            //Remove length/costs exceeding specified max transportable length
            //We clone the list first, so that the size can be changed without reloading the program.
            List<List<BoardCost>> localBrdFootCost = new List<List<BoardCost>>(g_brdFootCost);
            for (int i = 0; i < localBrdFootCost.Count; i++)
            {
                localBrdFootCost[i].RemoveAll(x => x.GetInches() > g_maxTransportableInches);
            }

            //Sort 
            List<ShoppingList> cutLists = new List<ShoppingList>();

            for (int i = 0; i < lengthsNeeded.Count; i++)
            {
                //Make sure we're looking at the longest needed length first.
                lengthsNeeded[i].Sort((x, y) => y.CompareTo(x));

                //Make sure that we can meet the needs of the longest board with our current length restriction.
                if (localBrdFootCost[i].Count == 0 && lengthsNeeded[i].Count > 0)
                {
                    MessageBox.Show(string.Format("No prices for requested board dimension {0}", i), "Parsing error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                ShoppingList optimalCutList = new ShoppingList();
                for (int j = 0; j < localBrdFootCost[i].Count; j++)
                {
                    //Make a temporary copy of the lengths needed list, so we can play with it multiple times.
                    List<double> currentNeededLengths = new List<double>(lengthsNeeded[i]);

                    //This is where we will stick the current list of cuts, based on the current starting length.
                    //We will then try and figure out whether this list is optimal for this board type.
                    ShoppingList dimensionedCutLists = new ShoppingList();
                    while (currentNeededLengths.Count > 0)
                    {
                        //We may need to change the current default board length, based on how long the cut needs to be.
                        int k = j;
                        while (currentNeededLengths[0] > (localBrdFootCost[i][k].GetInches()))
                        {
                            //Here we rely on performing the first operation first -- we check and make sure we can iterate backwards
                            //(the list is sorted largest to smallest). If we can -- we do, in the second operation, and then verify that
                            //this new length isn't greater than what we can transport. If it is..... exit. We can't achieve a solution.
                            if (k == 0)
                            {
                                MessageBox.Show(string.Format("Error! Requested board length larger than transportable size: {0} {1}", currentNeededLengths[0], g_maxTransportableInches), "Runtime error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                            k--;
                        }

                        List<double> cutList = new List<double>();
                        double remainder = FindMostEfficientUse(ref cutList, ref currentNeededLengths, localBrdFootCost[i][k].GetInches(), 0);

                        while (k < localBrdFootCost[i].Count - 1 && ((localBrdFootCost[i][k].GetInches() - localBrdFootCost[i][k + 1].GetInches()) <= remainder))
                        {
                            k++;
                        }

                        dimensionedCutLists.Add(new BoardCuts(localBrdFootCost[i][k], cutList));
                        foreach (double item in cutList)
                        {
                            currentNeededLengths.Remove(item);
                        }
                    }

                    //If the optimal list is empty, or the list just generated is lower in price than the current optimal list...
                    //replace the optimal list.
                    if (optimalCutList.Empty() || (dimensionedCutLists.GetCost() < optimalCutList.GetCost()))
                    {
                        optimalCutList = dimensionedCutLists;
                    }
                }

                cutLists.Add(optimalCutList);
            }

            double totalCost = 0;
            resultBox.Text = "";
            for (int i = 0; i < cutLists.Count; i++)
            {
                if (cutLists[i].GetBoards().Count == 0)
                {
                    continue;
                }

                if (doCutList)
                {
                    for (int j = 0; j < cutLists[i].GetBoards().Count; j++)
                    {

                        resultBox.Text += string.Format("{0}x{1}': ", GetEnumDescription((boardType)i), cutLists[i].GetBoards()[j].GetFeet());
                        foreach (double itr in cutLists[i].GetBoards()[j].GetCuts())
                        {
                            resultBox.Text += string.Format("{0} ", itr);
                        }
                        resultBox.Text += "\n";
                    }
                }
                else
                {
                    int lastFeet = 0;
                    int lastTotal = 0;
                    for (int j = 0; j < cutLists[i].GetBoards().Count; j++)
                    {
                        totalCost += cutLists[i].GetBoards()[j].GetCost();
                        if (cutLists[i].GetBoards()[j].GetFeet() != lastFeet)
                        {
                            if (lastFeet != 0)
                            {
                                resultBox.Text += string.Format("{0}x{1}' boards: {2}\n", GetEnumDescription((boardType)i), lastFeet, lastTotal);
                            }
                            lastFeet = cutLists[i].GetBoards()[j].GetFeet();
                            lastTotal = 1;
                        }
                        else
                        {
                            lastTotal++;
                        }
                    }
                    resultBox.Text += string.Format("{0}x{1}' boards: {2}\n", GetEnumDescription((boardType)i), lastFeet, lastTotal);
                }
            }
            if (!doCutList)
            {
                resultBox.Text += string.Format("Total cost is {0}", totalCost);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void ParseSaveFormat(string saveInput)
        {
            string[] dimensions = saveInput.Split('\n');

            List<List<double>> rehydratedList = new List<List<double>>((int)(boardType.FOUR_BY_FOUR) + 1);
            //Clear existing list.
            for (int i = 0; i < rehydratedList.Capacity; i++)
            {
                rehydratedList.Add(new List<double>(0));
            }

            for (int i = 0; i < dimensions.Length - 1; i += 2)
            {
                string[] dimension = dimensions[i].Split(':');
                boardType type = GetValueFromDescription<boardType>(dimension[0]);

                string[] lengths = dimensions[i + 1].Split(' ');
                List<double> lengthList = new List<double>();
                foreach (string length in lengths)
                {
                    double valueToAdd;
                    if (Double.TryParse(length, out valueToAdd))
                    {
                        lengthList.Add(valueToAdd);
                    }
                }
                rehydratedList[(int)type] = lengthList;

            }

            //Finally, now that we know we won't have any errors, go ahead and replace the current values...
            lengthsNeeded = rehydratedList;
            //And update the display.
            UpdateLengthsBox();
        }

        private string GetSaveFormat()
        {
            string saveString = "";
            saveString = "";
            for (int i = 0; i < lengthsNeeded.Capacity; i++)
            {
                if (lengthsNeeded[i].Capacity > 0)
                {
                    saveString += GetEnumDescription((boardType)i);
                    saveString += ":\n   ";
                    foreach (double value in lengthsNeeded[i])
                    {
                        saveString += value;
                        saveString += " ";
                    }
                    saveString += "\n";
                }
            }
            return saveString;
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (g_currentFileName == "")
            {
                saveAsToolStripMenuItem_Click(sender, e);
            }
            else
            {
                File.WriteAllText(g_currentFileName, GetSaveFormat());
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Stream myStream;
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            saveFileDialog1.Filter = "CLC files (*.clc)|*.clc";
            saveFileDialog1.FilterIndex = 2;
            saveFileDialog1.RestoreDirectory = true;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if ((myStream = saveFileDialog1.OpenFile()) != null)
                {
                    g_currentFileName = saveFileDialog1.FileName;
                    myStream.Close();

                    File.WriteAllText(g_currentFileName, GetSaveFormat());
                }
            }
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            lengthsNeeded.Clear();
            for (int i = 0; i < lengthsNeeded.Capacity; i++)
            {
                lengthsNeeded.Add(new List<double>(0));
            }
            UpdateLengthsBox();
            maxTransportBox.Clear();
            neededLengthsBox.Clear();
            boardTypeBox.ResetText();
            resultBox.Clear();
            g_currentFileName = "";
        }

        private void neededLengthsBox_TextChanged(object sender, EventArgs e)
        {
            double valueToAdd;
            if (!Double.TryParse(neededLengthsBox.Text, out valueToAdd))
                AddLengthsButton.Enabled = false;
            else
                AddLengthsButton.Enabled = true;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "CLC files (*.clc)|*.clc";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file
                    g_currentFileName = openFileDialog.FileName;

                    ParseSaveFormat(File.ReadAllText(g_currentFileName));
                }
            }
        }
    }
}
