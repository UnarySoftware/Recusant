using Netick.Unity;

namespace Unary.Core
{
    public class NetworkBehaviourExtended : NetworkBehaviour
    {
        private NetworkObject _networkObject;
        public NetworkObject NetworkObject
        {
            get
            {
                if (_networkObject == null)
                {
                    _networkObject = GetComponent<NetworkObject>();
                }

                return _networkObject;
            }
        }

        public bool GetInput<T>(out T input) where T : unmanaged
        {
            if (!IsInputSource || IsResimulating)
            {
                input = default;
                return false;
            }

            input = Sandbox.GetInput<T>(0);
            return true;
        }

        public void SetInput<T>(T input) where T : unmanaged
        {
            Sandbox.SetInput(input);
        }

        public bool FetchInputServer<T>(out T input) where T : unmanaged
        {
            if (Sandbox.IsClient)
            {
                input = default;
                return false;
            }

            bool fetch = FetchInput(out T result, out bool duplicated, 0);

            if (fetch && !duplicated)
            {
                input = result;
                return true;
            }
            else
            {
                input = default;
                return false;
            }
        }
    }
}
