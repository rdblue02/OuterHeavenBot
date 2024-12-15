namespace OuterHeavenBot.Core
{ 
    public interface ISearchHandler<TResult>
    {
        Task<IEnumerable<TResult>> SearchAsync(string query);
        IEnumerable<TResult> Search(string query);
    }  
}
