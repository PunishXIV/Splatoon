namespace Splatoon.SplatoonScripting
{
    internal record struct BlacklistData
    {
        internal string FullName;
        internal int Version;

        public BlacklistData(string fullName, int version)
        {
            FullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
            Version = version;
        }
    }
}
