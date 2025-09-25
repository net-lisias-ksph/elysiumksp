using KSP;

namespace ElysiumKSP.Optional
{
    public interface INetworkEvents
    {
        void OnVesselSync(Vessel vessel);
        void OnResourceSync(PartResource res, double oldAmount, double newAmount, Vessel v);
        void OnCustomMessage(string modId, string message);
    }
}