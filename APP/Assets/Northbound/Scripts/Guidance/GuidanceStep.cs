using System;

namespace Northbound.Guidance
{
    [Serializable]
    public struct GuidanceStep
    {
        public string chapter;
        public string locationName;
        public string objective;
        public string objectiveId;
        public string instruction;
        public string nextAction;
        public string destinationId;
        public string targetLocationId;
        public bool isMissionStart;
    }
}
