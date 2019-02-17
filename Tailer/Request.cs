namespace Tailer
{
    public struct Request
    {
        public string Page { get; }
        public string OriginalRequest { get; }
        public string Verb { get; }
        public string Path { get; }

        public Request(string request, string verb, string path, string page)
        {
            OriginalRequest = request;
            Page = page;
            Verb = verb;
            Path = path;
        }
    }
}