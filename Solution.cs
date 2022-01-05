public class Solution
{
    public string Word;
    // +1 as the % operator used to access means a = 1, b=2 
    // this is fewer operations than correcting by 1 every array index access
    private byte[] LetterCounts = new byte[Constants.validCharacters.Length + 1];

    public int GetLetterCount(char c)
    {
        return LetterCounts[(int)c % 32];
    }

    public Solution(string word)
    {
        Word = word;

        for (var i = 0; i < Constants.wordLength; i++)
        {
            LetterCounts[(int)word[i] % 32]++;
        }
    }

    public override string ToString()
    {
        return Word;
    }
}