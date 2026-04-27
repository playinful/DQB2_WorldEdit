namespace EyeOfRubiss
{
    /// <summary> Static class holding constant values to be used in various places. </summary>
    public static class Constants
    {
        public const ushort BLOCK_AIR = 0;
        public const ushort BLOCK_BEDROCK = 1;
        public const ushort BLOCK_EARTH = 2;

        public const ulong VOXEL_AIR = 0;
        public const ulong VOXEL_SEAFLOOR = 8;
        public const ulong VOXEL_FLOOR_COLLISION = 603;
        public const ulong VOXEL_BLUEPRINT = 599;
        public const ulong VOXEL_DEFAULT = 2;
        public const ulong VOXEL_UNKNOWN = 7;
        public const ulong VOXEL_PARTSBLOCK = 4;
        public const ulong VOXEL_TERRAIN_COLLISION = 600;
        public const ulong VOXEL_FLUID_COLLISION = 601;
        public const ulong VOXEL_FLUID_PARTSBLOCK_COLLISION = 602;

        /// <summary> Static class holding constant values pertaining to hexidecimal colour strings. </summary>
        public static class Colors
        {
            /// <summary> The colour used to display items dyed white in the GUI. </summary>
            public const string WHITE = "FFFFFF";
            /// <summary> The colour used to display items dyed black in the GUI. </summary>
            public const string BLACK = "000000";
            /// <summary> The colour used to display items dyed purple in the GUI. </summary>
            public const string PURPLE = "800080";
            /// <summary> The colour used to display items dyed pink in the GUI. </summary>
            public const string PINK = "FF0094";
            /// <summary> The colour used to display items dyed red in the GUI. </summary>
            public const string RED = "FF0000";
            /// <summary> The colour used to display items dyed green in the GUI. </summary>
            public const string GREEN = "00FF00";
            /// <summary> The colour used to display items dyed yellow in the GUI. </summary>
            public const string YELLOW = "FFFF00";
            /// <summary> The colour used to display items dyed blue in the GUI. </summary>
            public const string BLUE = "0000FF";
        }
        /// <summary> Static class holding string identifiers for program controls. </summary>
        public static class Controls
        {
            public const string BRUSH_PRIMARY = "brush_primary";
            public const string BRUSH_SECONDARY = "brush_secondary";
            public const string BRUSH_TERTIARY = "brush_tertiary";
            public const string DELETE = "delete";

            public const string CAMERA_LEFT = "camera_left";
            public const string CAMERA_RIGHT = "camera_right";
            public const string CAMERA_FORWARD = "camera_forward";
            public const string CAMERA_BACK = "camera_back";
            public const string CAMERA_UP = "camera_up";
            public const string CAMERA_DOWN = "camera_down";
            public const string CAMERA_SPEED_UP = "camera_speed_up";
            public const string CAMERA_SPEED_DOWN = "camera_speed_down";
            public const string CAMERA_FOV_UP = "camera_fov_up";
            public const string CAMERA_FOV_DOWN = "camera_fov_down";
            public const string CAMERA_ISOMETRIC = "camera_isometric";
            public const string RESET_CAMERA = "reset_camera";
            public const string CURSOR_CAPTURE = "cursor_capture";
            public const string CURSOR_RELEASE = "cursor_release";
            public const string CAMERA_HOLD_TO_MOVE = "camera_hold_to_move";

            public const string SCREENSHOT = "screenshot";

            public const string KEYBOARD_SHORTCUT_OPEN_FILE = "keyboard_shortcut_open_file";
            public const string KEYBOARD_SHORTCUT_OPEN_FOLDER = "keyboard_shortcut_open_folder";
            public const string KEYBOARD_SHORTCUT_SAVE = "keyboard_shortcut_save";
            public const string KEYBOARD_SHORTCUT_CLOSE = "keyboard_shortcut_close";
            public const string KEYBOARD_SHORTCUT_COPY = "keyboard_shortcut_copy";
            public const string KEYBOARD_SHORTCUT_CUT = "keyboard_shortcut_cut";
            public const string KEYBOARD_SHORTCUT_PASTE = "keyboard_shortcut_paste";
            public const string KEYBOARD_SHORTCUT_FILL = "keyboard_shortcut_fill";

            public const string PROP_EDITOR = "prop_editor";

            public const string TEST1 = "test_1";
            public const string TEST2 = "test_2";
            public const string TEST3 = "test_3";
        }
    }
}