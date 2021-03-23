using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace FFRKOriginSearch
{
    public class VisualForm : Form
    {
        public VisualForm(IDictionary<string, (int, IList<string>)> soulBreaks) : base()
        {
            Size = new Size(800, 600);
            StartPosition = FormStartPosition.CenterScreen;

            AddSelectionControls(soulBreaks.Keys.Where(k => soulBreaks[k].Item2.Count > 0).ToArray(), soulBreaks);

            BringToFront();
            Focus();
            Show();
        }

        private void AddSelectionControls(string[] characters, IDictionary<string, (int, IList<string>)> soulBreaks)
        {
            Controls.Clear();

            TableLayoutPanel menu = new TableLayoutPanel()
            {
                Width = Width * 2 / 3,
                Height = Height * 4 / 5,
                Left = (ClientSize.Width - Width * 2 / 3) / 2,
                Top = (ClientSize.Height - Height * 4 / 5) / 2,
                Margin = Padding.Empty,
                Padding = Padding.Empty
        };

            TextBox soulBreakListDisplay = new TextBox()
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                Margin = Padding.Empty,
                TabStop = false,
                BackColor = Color.White,
                ForeColor = Color.Black,
                Font = new Font(FontFamily.GenericMonospace, 12),
                Multiline = true,
                WordWrap = false,
                ScrollBars = ScrollBars.Both,
                ReadOnly = true
        };

            ComboBox characterList = new ComboBox()
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = Padding.Empty,
                TabStop = false,
                Font = new Font(FontFamily.GenericMonospace, 8)
            };

            //Orders by realm and then by character name
            characterList.Items.AddRange(characters.Where(name => !name.Equals(OfficialSiteCounter.HeroAbilities))
                                                   .OrderBy(c => soulBreaks[c].Item1)
                                                   .ToArray());
            if(characters.Contains(OfficialSiteCounter.HeroAbilities))
                characterList.Items.Add("Hero Abilities");
            if(characterList.Items.Count != 0)
            {
                characterList.SelectedIndex = 0;
                characterList.SelectedIndexChanged += (o, s) => {
                    if (characterList.SelectedItem.ToString().Equals("Hero Abilities"))
                        UpdateListedSoulBreaks(soulBreakListDisplay, soulBreaks[OfficialSiteCounter.HeroAbilities].Item2);
                    else
                        UpdateListedSoulBreaks(soulBreakListDisplay, soulBreaks[characterList.SelectedItem.ToString()].Item2);
                };

                UpdateListedSoulBreaks(soulBreakListDisplay, soulBreaks[characterList.SelectedItem.ToString()].Item2);
            }
            else
            {
                characterList.Items.Add("");
                UpdateListedSoulBreaks(soulBreakListDisplay, new List<string>());
            }

            menu.Controls.Add(characterList);
            menu.Controls.Add(soulBreakListDisplay);

            menu.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            menu.RowStyles.Add(new RowStyle(SizeType.Percent, 75));

            Controls.Add(menu);
        }

        private void UpdateListedSoulBreaks(TextBox listControl, IList<string> soulBreakNames)
        {
            if (soulBreakNames.Count == 0)
                listControl.Text = "All soul breaks, record materia, legend materia, and hero abilities currently released in Global are in the spreadsheet.";
            else
                listControl.Text = string.Join(System.Environment.NewLine, soulBreakNames.Select(str => str.Replace("&#39;", "'")));
        }
    }
}
