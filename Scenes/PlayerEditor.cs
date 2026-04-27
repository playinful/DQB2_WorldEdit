using EyeOfRubiss.Nodes;
using Godot;
using System;

namespace EyeOfRubiss.Scenes
{
    public partial class PlayerEditor : Control
    {
        [ExportGroup("Scene Elements")]
        [Export] private LineEdit _PlayerName_LineEdit;

        [Export] private SpinBox _HP_SpinBox;
        [Export] private SpinBox _AdditionalHP_SpinBox;
        [Export] private SpinBox _Experience_SpinBox;
        [Export] private SpinBox _Level_SpinBox;
        [Export] private SpinBox _Hunger_SpinBox;
        [Export] private SpinBox _Stamina_SpinBox;
        [Export] private SpinBox _Attack_SpinBox;
        [Export] private SpinBox _Defence_SpinBox;

        [Export] private OptionButton _Sex_OptionButton;

        [Export] private Button _HairColor_Button;
        [Export] private Button _SkinColor_Button;
        // [Export] private Button _EyeColor_Button;

        // [Export] private ItemButton _Weapon_ItemButton;
        // [Export] private ItemButton _Shield_ItemButton;
        // [Export] private ItemButton _Armor_ItemButton;
        // [Export] private ItemButton _Hammer_ItemButton;
        // [Export] private ItemButton _Gloves_ItemButton;
        // [Export] private ItemButton _BottomlessPot_ItemButton;
        // [Export] private ItemButton _EchoFlute_ItemButton;
        // [Export] private ItemButton _TransformOTrowel_ItemButton;
        // [Export] private ItemButton _MagicPencil_ItemButton;
        // [Export] private ItemButton _Chisel_ItemButton;
        // [Export] private ItemButton _FishingRod_ItemButton;

        // [Export] private ItemButton _Glamour_Weapon_ItemButton;
        // [Export] private ItemButton _Glamour_Shield_ItemButton;
        // [Export] private ItemButton _Glamour_Armor_ItemButton;
        // [Export] private ItemButton _Glamour_Hammer_ItemButton;
        // [Export] private ItemButton _Glamour_Headwear_ItemButton;
        // [Export] private ItemButton _Glamour_Accessory1_ItemButton;
        // [Export] private ItemButton _Glamour_Accessory2_ItemButton;
        // [Export] private ItemButton _Glamour_Accessory3_ItemButton;

        /*
        public void UpdateAll()
        {
            if (!CommonData.HasInstance())
                return;

            _PlayerName_LineEdit.Text = CommonData.Instance.PlayerName;

            _HP_SpinBox.SetValueNoSignal(CommonData.Instance.PlayerHP);
            _AdditionalHP_SpinBox.SetValueNoSignal(CommonData.Instance.PlayerAdditionalHP);
            _Attack_SpinBox.SetValueNoSignal(CommonData.Instance.PlayerAttack);
            _Defence_SpinBox.SetValueNoSignal(CommonData.Instance.PlayerDefence);
            _Level_SpinBox.SetValueNoSignal(CommonData.Instance.PlayerLevel);
            _Experience_SpinBox.SetValueNoSignal(CommonData.Instance.PlayerExperience);
            _Stamina_SpinBox.SetValueNoSignal(CommonData.Instance.PlayerStamina);
            _Hunger_SpinBox.SetValueNoSignal(CommonData.Instance.PlayerHunger / 100);

            InventoryItem item = CommonData.Instance.PlayerWeapon;
            if (item.ItemID == 0)
                _Weapon_ItemButton.SetItem("Weapon", 2362);
            else
                _Weapon_ItemButton.SetItem(item.GetInfo());

            item = CommonData.Instance.PlayerShield;
            if (item.ItemID == 0)
                _Shield_ItemButton.SetItem("Shield", 2363);
            else
                _Shield_ItemButton.SetItem(item.GetInfo());

            item = CommonData.Instance.PlayerArmour;
            if (item.ItemID == 0)
                _Armor_ItemButton.SetItem("Armour", 2364);
            else
                _Armor_ItemButton.SetItem(item.GetInfo());

            item = CommonData.Instance.PlayerHammer;
            if (item.ItemID == 0)
                _Hammer_ItemButton.SetItem("Hammer", 2365);
            else
                _Hammer_ItemButton.SetItem(item.GetInfo());

            item = CommonData.Instance.PlayerGloves;
            if (item.ItemID == 0)
                _Gloves_ItemButton.SetItem("Gloves", 2366);
            else
                _Gloves_ItemButton.SetItem(item.GetInfo());
            item = CommonData.Instance.PlayerBottomlessPot;
            if (item.ItemID == 0)
                _BottomlessPot_ItemButton.SetItem("Bottomless Pot", 2367);
            else
                _BottomlessPot_ItemButton.SetItem(item.GetInfo());
            item = CommonData.Instance.PlayerEchoFlute;
            if (item.ItemID == 0)
                _EchoFlute_ItemButton.SetItem("Echo Flute", 2368);
            else
                _EchoFlute_ItemButton.SetItem(item.GetInfo());
            item = CommonData.Instance.PlayerTransformOTrowel;
            if (item.ItemID == 0)
                _TransformOTrowel_ItemButton.SetItem("Transform-O-Trowel", 2369);
            else
                _TransformOTrowel_ItemButton.SetItem(item.GetInfo());
            item = CommonData.Instance.PlayerMagicPencil;
            if (item.ItemID == 0)
                _MagicPencil_ItemButton.SetItem("Magic Pencil", 2370);
            else
                _MagicPencil_ItemButton.SetItem(item.GetInfo());
            item = CommonData.Instance.PlayerChisel;
            if (item.ItemID == 0)
                _Chisel_ItemButton.SetItem("Chisel", 2371);
            else
                _Chisel_ItemButton.SetItem(item.GetInfo());
            item = CommonData.Instance.PlayerFishingRod;
            if (item.ItemID == 0)
                _FishingRod_ItemButton.SetItem("Fishing Rod", 2372);
            else
                _FishingRod_ItemButton.SetItem(item.GetInfo());

            item = CommonData.Instance.GlamourWeapon;
            if (item.ItemID == 0)
                _Glamour_Weapon_ItemButton.SetItem("Weapon Appearance", 2362);
            else
                _Glamour_Weapon_ItemButton.SetItem(item.GetInfo());
            item = CommonData.Instance.GlamourShield;
            if (item.ItemID == 0)
                _Glamour_Shield_ItemButton.SetItem("Shield Appearance", 2363);
            else
                _Glamour_Shield_ItemButton.SetItem(item.GetInfo());
            item = CommonData.Instance.GlamourArmour;
            if (item.ItemID == 0)
                _Glamour_Armor_ItemButton.SetItem("Armour Appearance", 2364);
            else
                _Glamour_Armor_ItemButton.SetItem(item.GetInfo());
            item = CommonData.Instance.GlamourHammer;
            if (item.ItemID == 0)
                _Glamour_Hammer_ItemButton.SetItem("Hammer Appearance", 2365);
            else
                _Glamour_Hammer_ItemButton.SetItem(item.GetInfo());
            item = CommonData.Instance.GlamourHeadwear;
            if (item.ItemID == 0)
                _Glamour_Headwear_ItemButton.SetItem("Hair/Hat", 2374);
            else
                _Glamour_Headwear_ItemButton.SetItem(item.GetInfo());
            item = CommonData.Instance.GlamourAccessory1;
            if (item.ItemID == 0)
                _Glamour_Accessory1_ItemButton.SetItem("Accessory", 2373);
            else
                _Glamour_Accessory1_ItemButton.SetItem(item.GetInfo());
            item = CommonData.Instance.GlamourAccessory2;
            if (item.ItemID == 0)
                _Glamour_Accessory2_ItemButton.SetItem("Accessory", 2373);
            else
                _Glamour_Accessory2_ItemButton.SetItem(item.GetInfo());
            item = CommonData.Instance.GlamourAccessory3;
            if (item.ItemID == 0)
                _Glamour_Accessory3_ItemButton.SetItem("Accessory", 2373);
            else
                _Glamour_Accessory3_ItemButton.SetItem(item.GetInfo());

            _Sex_OptionButton.Selected = CommonData.Instance.PlayerSex ? 0 : 1;
        }

        public void _On_PlayerName_LineEdit_TextChanged(string new_text)
        {
            if (!CommonData.HasInstance())
                return;

            CommonData.Instance.PlayerName = new_text;
            _PlayerName_LineEdit.Text = CommonData.Instance.PlayerName;
        }

        public void _On_PlayerHP_SpinBox_ValueChanged(float value)
        {
            if (!CommonData.HasInstance())
                return;

            CommonData.Instance.PlayerHP = (short)Math.Round(value);
            // It's kind of pointless to edit the current HP by itself so here we just refill it whenever it changes
            CommonData.Instance.PlayerCurrentHP = CommonData.Instance.PlayerHP;
        }
        public void _On_AdditionalHP_SpinBox_ValueChanged(float value)
        {
            if (!CommonData.HasInstance())
                return;

            CommonData.Instance.PlayerAdditionalHP = (short)Math.Round(value);
        }
        public void _On_Experience_SpinBox_ValueChanged(float value)
        {
            if (!CommonData.HasInstance())
                return;

            CommonData.Instance.PlayerExperience = (short)Math.Round(value); // todo int
        }
        public void _On_Level_SpinBox_ValueChanged(float value)
        {
            if (!CommonData.HasInstance())
                return;

            CommonData.Instance.PlayerLevel = (byte)Math.Round(value);
        }
        public void _On_Hunger_SpinBox_ValueChanged(float value)
        {
            if (!CommonData.HasInstance())
                return;

            CommonData.Instance.PlayerHunger = (short)Math.Round(value * 100);
        }
        public void _On_Stamina_SpinBox_ValueChanged(float value)
        {
            if (!CommonData.HasInstance())
                return;

            CommonData.Instance.PlayerStamina = (short)Math.Round(value);
        }
        public void _On_Attack_SpinBox_ValueChanged(float value)
        {
            if (!CommonData.HasInstance())
                return;

            CommonData.Instance.PlayerAttack = (short)Math.Round(value);
        }
        public void _On_Defence_SpinBox_ValueChanged(float value)
        {
            if (!CommonData.HasInstance())
                return;

            CommonData.Instance.PlayerDefence = (short)Math.Round(value);
        }

        public void _On_Sex_OptionButton_ItemSelected(int index)
        {
            if (!CommonData.HasInstance())
                return;

            switch (index)
            {
                case 0:
                    CommonData.Instance.PlayerSex = true;
                    break;
                case 1:
                    CommonData.Instance.PlayerSex = false;
                    break;
            }
        }
    */
    }
}