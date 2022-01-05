public class Word
{
    public string Letters;
    
    // the word with precomputed % 32 letters
    public int[] offsetLetters; 

    // +1 as the % operator used to access means a = 1, b=2 
    // this is fewer operations than correcting by 1 every array index access
    private byte[] LetterCounts = new byte[Constants.validCharacters.Length + 1];

    public int GetLetterCount(int offsetLetter)
    {
        return LetterCounts[offsetLetter];
    }

    public Word(string letters)
    {
        Letters = letters;
        offsetLetters = new int[Constants.wordLength];
        for (var i = 0; i < Constants.wordLength; i++)
        {
            var letterModulo = letters[i] % 32;
            offsetLetters[i] = letterModulo;
            LetterCounts[letterModulo]++;
        }
    }

    public override string ToString()
    {
        return Letters;
    }

    public override bool Equals(object? obj) => obj is Word other && this.Equals(other);

    public bool Equals(Word w) => Letters == w.Letters;

    public override int GetHashCode() => Letters.GetHashCode();

    public static bool operator ==(Word lhs, Word rhs) => lhs.Equals(rhs);

    public static bool operator !=(Word lhs, Word rhs) => !(lhs == rhs);
}