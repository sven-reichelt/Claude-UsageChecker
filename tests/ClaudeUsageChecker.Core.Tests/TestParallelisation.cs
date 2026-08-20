using Xunit;

// The selected language is process-wide state: Localizer.Use switches it for
// everyone, and it sets the culture along with it. Test classes running side by
// side therefore pull the rug from under each other - a class walking through
// all nine languages leaves German set while another is comparing English text.
//
// It struck the moment the first such test arrived here: two entirely unrelated
// classes went red, one about the OAuth flow and one about the order of usage
// windows. A race that shows up as a failure only sometimes is worse than a
// slower run, so the App test assembly gave up parallelisation for the same
// reason.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
