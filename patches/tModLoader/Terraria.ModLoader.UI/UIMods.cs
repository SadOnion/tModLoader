using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;
using Terraria.UI.Gamepad;
using Newtonsoft.Json;
using System.Reflection;
using Terraria.ModLoader.Config;

namespace Terraria.ModLoader.UI
{
	internal class UIMods : UIState
	{
		private UIElement uIElement;
		private UIPanel uIPanel;
		private UILoaderAnimatedImage uiLoader;
		private bool needToRemoveLoading;
		private UIList modList;
		private readonly List<UIModItem> items = new List<UIModItem>();
		private bool updateNeeded;
		public bool loading;
		private UIInputTextField filterTextBox;
		public UICycleImage SearchFilterToggle;
		public ModsMenuSortMode sortMode = ModsMenuSortMode.RecentlyUpdated;
		public EnabledFilter enabledFilterMode = EnabledFilter.All;
		public ModSideFilter modSideFilterMode = ModSideFilter.All;
		public SearchFilter searchFilterMode = SearchFilter.Name;
		internal readonly List<UICycleImage> _categoryButtons = new List<UICycleImage>();
		internal string filter;
		private UIAutoScaleTextTextPanel<string> buttonEA;
		private UIAutoScaleTextTextPanel<string> buttonDA;
		private UIAutoScaleTextTextPanel<string> buttonRM;
		private UIAutoScaleTextTextPanel<string> buttonB;
		private UIAutoScaleTextTextPanel<string> buttonOMF;
		private UIAutoScaleTextTextPanel<string> buttonMP;

		public override void OnInitialize() {
			uIElement = new UIElement {
				Width = { Percent = 0.8f },
				MaxWidth = UICommon.MaxPanelWidth,
				Top = { Pixels = 220 },
				Height = { Pixels = -220, Percent = 1f },
				HAlign = 0.5f
			};

			uIPanel = new UIPanel {
				Width = { Percent = 1f },
				Height = { Pixels = -110, Percent = 1f },
				BackgroundColor = UICommon.mainPanelBackground,
				PaddingTop = 0f
			};
			uIElement.Append(uIPanel);

			uiLoader = new UILoaderAnimatedImage(0.5f, 0.5f, 1f);

			modList = new UIList {
				Width = { Pixels = -25, Percent = 1f },
				Height = { Pixels = -50, Percent = 1f },
				Top = { Pixels = 50 },
				ListPadding = 5f
			};
			uIPanel.Append(modList);

			var uIScrollbar = new UIScrollbar {
				Height = { Pixels = -50, Percent = 1f },
				Top = { Pixels = 50 },
				HAlign = 1f
			}.WithView(100f, 1000f);
			uIPanel.Append(uIScrollbar);

			modList.SetScrollbar(uIScrollbar);

			var uIHeaderTexTPanel = new UITextPanel<string>(Language.GetTextValue("tModLoader.ModsModsList"), 0.8f, true) {
				HAlign = 0.5f,
				Top = { Pixels = -35 },
				BackgroundColor = UICommon.defaultUIBlue
			}.WithPadding(15f);
			uIElement.Append(uIHeaderTexTPanel);

			buttonEA = new UIAutoScaleTextTextPanel<string>(Language.GetTextValue("tModLoader.ModsEnableAll")) {
				TextColor = Color.Green,
				Width = new StyleDimension(-10f, 1f / 3f),
				Height = { Pixels = 40 },
				VAlign = 1f,
				Top = { Pixels = -65 }
			}.WithFadedMouseOver();
			buttonEA.OnClick += EnableAll;
			uIElement.Append(buttonEA);

			// TODO CopyStyle doesn't capture all the duplication here, consider an inner method
			buttonDA = new UIAutoScaleTextTextPanel<string>(Language.GetTextValue("tModLoader.ModsDisableAll"));
			buttonDA.CopyStyle(buttonEA);
			buttonDA.TextColor = Color.Red;
			buttonDA.HAlign = 0.5f;
			buttonDA.WithFadedMouseOver();
			buttonDA.OnClick += DisableAll;
			uIElement.Append(buttonDA);

			buttonRM = new UIAutoScaleTextTextPanel<string>(Language.GetTextValue("tModLoader.ModsReloadMods"));
			buttonRM.CopyStyle(buttonEA);
			buttonRM.HAlign = 1f;
			buttonDA.WithFadedMouseOver();
			buttonRM.OnClick += ReloadMods;
			uIElement.Append(buttonRM);

			buttonB = new UIAutoScaleTextTextPanel<string>(Language.GetTextValue("UI.Back"));
			buttonB.CopyStyle(buttonEA);
			buttonB.Top.Pixels = -20;
			buttonB.WithFadedMouseOver();
			buttonB.OnClick += BackClick;

			uIElement.Append(buttonB);
			buttonOMF = new UIAutoScaleTextTextPanel<string>(Language.GetTextValue("tModLoader.ModsOpenModsFolder"));
			buttonOMF.CopyStyle(buttonB);
			buttonOMF.HAlign = 0.5f;
			buttonOMF.WithFadedMouseOver();
			buttonOMF.OnClick += OpenModsFolder;
			uIElement.Append(buttonOMF);

			var texture = Texture2D.FromStream(Main.instance.GraphicsDevice, Assembly.GetExecutingAssembly().GetManifestResourceStream("Terraria.ModLoader.UI.UIModBrowserIcons.png"));
			var upperMenuContainer = new UIElement {
				Width = { Percent = 1f },
				Height = { Pixels = 32 },
				Top = { Pixels = 10 }
			};

			UICycleImage toggleImage;
			for (int j = 0; j < 3; j++) {
				if (j == 0) { //TODO: ouch, at least there's a loop but these click events look quite similar
					toggleImage = new UICycleImage(texture, 3, 32, 32, 34 * 3, 0);
					toggleImage.setCurrentState((int)sortMode);
					toggleImage.OnClick += (a, b) => {
						sortMode = sortMode.NextEnum();
						updateNeeded = true;
					};
					toggleImage.OnRightClick += (a, b) => {
						sortMode = sortMode.PreviousEnum();
						updateNeeded = true;
					};
				}
				else if (j == 1) {
					toggleImage = new UICycleImage(texture, 3, 32, 32, 34 * 4, 0);
					toggleImage.setCurrentState((int)enabledFilterMode);
					toggleImage.OnClick += (a, b) => {
						enabledFilterMode = enabledFilterMode.NextEnum();
						updateNeeded = true;
					};
					toggleImage.OnRightClick += (a, b) => {
						enabledFilterMode = enabledFilterMode.PreviousEnum();
						updateNeeded = true;
					};
				}
				else {
					toggleImage = new UICycleImage(texture, 5, 32, 32, 34 * 5, 0);
					toggleImage.setCurrentState((int)modSideFilterMode);
					toggleImage.OnClick += (a, b) => {
						modSideFilterMode = modSideFilterMode.NextEnum();
						updateNeeded = true;
					};
					toggleImage.OnRightClick += (a, b) => {
						modSideFilterMode = modSideFilterMode.PreviousEnum();
						updateNeeded = true;
					};
				}
				toggleImage.Left.Pixels = j * 36 + 8;
				_categoryButtons.Add(toggleImage);
				upperMenuContainer.Append(toggleImage);
			}

			var filterTextBoxBackground = new UIPanel {
				Top = { Percent = 0f },
				Left = { Pixels = -170, Percent = 1f },
				Width = { Pixels = 135 },
				Height = { Pixels = 40 }
			};
			filterTextBoxBackground.OnRightClick += (a, b) => filterTextBox.Text = "";
			upperMenuContainer.Append(filterTextBoxBackground);

			filterTextBox = new UIInputTextField(Language.GetTextValue("tModLoader.ModsTypeToSearch")) {
				Top = { Pixels = 5 },
				Left = { Pixels = -160, Percent = 1f },
				Width = { Pixels = 120 },
				Height = { Pixels = 20 }
			};
			filterTextBox.OnTextChange += (a, b) => updateNeeded = true;
			upperMenuContainer.Append(filterTextBox);

			SearchFilterToggle = new UICycleImage(texture, 2, 32, 32, 34 * 2, 0) {
				Left = { Pixels = 545 }
			};
			SearchFilterToggle.setCurrentState((int)searchFilterMode);
			SearchFilterToggle.OnClick += (a, b) => {
				searchFilterMode = searchFilterMode.NextEnum();
				updateNeeded = true;
			};
			SearchFilterToggle.OnRightClick += (a, b) => {
				searchFilterMode = searchFilterMode.PreviousEnum();
				updateNeeded = true;
			};
			_categoryButtons.Add(SearchFilterToggle);
			upperMenuContainer.Append(SearchFilterToggle);

			buttonMP = new UIAutoScaleTextTextPanel<string>(Language.GetTextValue("tModLoader.ModsModPacks"));
			buttonMP.CopyStyle(buttonOMF);
			buttonMP.HAlign = 1f;
			buttonMP.WithFadedMouseOver();
			buttonMP.OnClick += GotoModPacksMenu;
			uIElement.Append(buttonMP);

			uIPanel.Append(upperMenuContainer);
			Append(uIElement);
		}

		private static void BackClick(UIMouseEvent evt, UIElement listeningElement) {
			Main.PlaySound(11, -1, -1, 1);
			// To prevent entering the game with Configs that violate ReloadRequired
			if (ConfigManager.AnyModNeedsReload())
			{
				Main.menuMode = Interface.reloadModsID;
				return;
			}
			Main.menuMode = 0;
		}

		private void ReloadMods(UIMouseEvent evt, UIElement listeningElement) {
			Main.PlaySound(10, -1, -1, 1);
			if (items.Count > 0)
				ModLoader.Reload();
		}

		private static void OpenModsFolder(UIMouseEvent evt, UIElement listeningElement) {
			Main.PlaySound(10, -1, -1, 1);
			Directory.CreateDirectory(ModLoader.ModPath);
			Process.Start(ModLoader.ModPath);
		}

		private static void GotoModPacksMenu(UIMouseEvent evt, UIElement listeningElement) {
			if (!Interface.modsMenu.loading) {
				Main.PlaySound(12, -1, -1, 1);
				Main.menuMode = Interface.modPacksMenuID;
			}
		}

		private void EnableAll(UIMouseEvent evt, UIElement listeningElement) {
			Main.PlaySound(12, -1, -1, 1);
			foreach (UIModItem modItem in items) {
				modItem.Enable();
			}
		}

		private void DisableAll(UIMouseEvent evt, UIElement listeningElement) {
			Main.PlaySound(12, -1, -1, 1);
			foreach (UIModItem modItem in items) {
				modItem.Disable();
			}
		}

		public override void Update(GameTime gameTime) {
			base.Update(gameTime);
			if (needToRemoveLoading) {
				needToRemoveLoading = false;
				uIPanel.RemoveChild(uiLoader);
			}
			if (!updateNeeded) return;
			updateNeeded = false;
			filter = filterTextBox.Text;
			modList.Clear();
			modList.AddRange(items.Where(item => item.PassFilters()));
		}

		public override void Draw(SpriteBatch spriteBatch) {
			base.Draw(spriteBatch);
			for (int i = 0; i < this._categoryButtons.Count; i++) {
				if (this._categoryButtons[i].IsMouseHovering) {
					string text;
					switch (i) {
						case 0:
							text = sortMode.ToFriendlyString();
							break;
						case 1:
							text = enabledFilterMode.ToFriendlyString();
							break;
						case 2:
							text = modSideFilterMode.ToFriendlyString();
							break;
						case 3:
							text = searchFilterMode.ToFriendlyString();
							break;
						default:
							text = "None";
							break;
					}
					//TODO common pattern
					float x = Main.fontMouseText.MeasureString(text).X;
					Vector2 vector = new Vector2((float)Main.mouseX, (float)Main.mouseY) + new Vector2(16f);
					if (vector.Y > (float)(Main.screenHeight - 30)) {
						vector.Y = (float)(Main.screenHeight - 30);
					}
					if (vector.X > (float)Main.screenWidth - x) {
						vector.X = (float)(Main.screenWidth - x - 30);
					}
					Utils.DrawBorderStringFourWay(spriteBatch, Main.fontMouseText, text, vector.X, vector.Y, new Color((int)Main.mouseTextColor, (int)Main.mouseTextColor, (int)Main.mouseTextColor, (int)Main.mouseTextColor), Color.Black, Vector2.Zero, 1f);
					return;
				}
			}
			UILinkPointNavigator.Shortcuts.BackButtonCommand = 1;
		}

		public override void OnActivate() {
			Main.clrInput();
			modList.Clear();
			items.Clear();
			loading = true;
			uIPanel.Append(uiLoader);
			ConfigManager.LoadAll(); // Makes sure MP configs are cleared.
			Populate();
		}

		internal void Populate() {
			Task.Factory
				.StartNew(ModOrganizer.FindMods)
				.ContinueWith(task => {
					var mods = task.Result;
					foreach (var mod in mods) {
						UIModItem modItem = new UIModItem(mod);
						items.Add(modItem);
					}
					needToRemoveLoading = true;
					updateNeeded = true;
					loading = false;
				}, TaskScheduler.FromCurrentSynchronizationContext());
		}
	}

	public static class ModsMenuSortModesExtensions
	{
		public static string ToFriendlyString(this ModsMenuSortMode sortmode) {
			switch (sortmode) {
				case ModsMenuSortMode.RecentlyUpdated:
					return Language.GetTextValue("tModLoader.ModsSortRecently");
				case ModsMenuSortMode.DisplayNameAtoZ:
					return Language.GetTextValue("tModLoader.ModsSortNamesAlph");
				case ModsMenuSortMode.DisplayNameZtoA:
					return Language.GetTextValue("tModLoader.ModsSortNamesReverseAlph");
			}
			return "Unknown Sort";
		}
	}

	public static class EnabledFilterModesExtensions
	{
		public static string ToFriendlyString(this EnabledFilter updateFilterMode) {
			switch (updateFilterMode) {
				case EnabledFilter.All:
					return Language.GetTextValue("tModLoader.ModsShowAllMods");
				case EnabledFilter.EnabledOnly:
					return Language.GetTextValue("tModLoader.ModsShowEnabledMods");
				case EnabledFilter.DisabledOnly:
					return Language.GetTextValue("tModLoader.ModsShowDisabledMods");
			}
			return "Unknown Sort";
		}
	}

	public enum ModsMenuSortMode
	{
		RecentlyUpdated,
		DisplayNameAtoZ,
		DisplayNameZtoA,
	}

	public enum EnabledFilter
	{
		All,
		EnabledOnly,
		DisabledOnly,
	}
}