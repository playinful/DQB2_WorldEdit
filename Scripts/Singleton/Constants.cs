namespace EyeOfRubiss
{
    /// <summary> Static class holding constant values to be used in various places. </summary>
    public static class Constants
    {
        /// <summary> The block ID representing air (or, empty space) in DQB2. </summary>
        public const ushort BLOCK_AIR = 0;
        /// <summary> The block ID representing bedrock in DQB2. </summary>
        public const ushort BLOCK_BEDROCK = 1;

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
            public const string CAMERA_LEFT = "camera_left";
            public const string CAMERA_RIGHT = "camera_right";
            public const string CAMERA_FORWARD = "camera_forward";
            public const string CAMERA_BACK = "camera_back";
            public const string CAMERA_UP = "camera_up";
            public const string CAMERA_DOWN = "camera_down";
            public const string CAMERA_SPEED_UP = "camera_speed_up";
            public const string CAMERA_SPEED_DOWN = "camera_speed_down";
            public const string CURSOR_RELEASE = "cursor_release";
            public const string RESET_CAMERA = "reset_camera";
        }
    }
}