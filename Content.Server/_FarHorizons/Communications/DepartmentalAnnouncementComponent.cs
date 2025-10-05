using Content.Shared.Communications;

namespace Content.Server.Communications
{
    [RegisterComponent]
    
    public sealed partial class DepartmentalAnnouncementComponent : SharedCommunicationsConsoleComponent
    { 
        /// <summary>
        /// Fluent ID for the announcement title
        /// If a Fluent ID isn't found, just uses the raw string
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField(required: true)]
        public LocId TitleAlt = "comms-console-announcement-title-station-alt";
        public float UIUpdateAccumulator = 0f;
        /// <summary>
        ///     List of channels available to a communication console.
        /// </summary>
        [DataField]
        public List<string> Channels = new List<string> { "No Channels Available" };

        /// <summary>
        ///     This is the default channel for the communication console.
        /// </summary>
        [DataField]
        public string CurrentChannel = "No Channels Available";
    }
}