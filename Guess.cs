using System.Text;

public class Guess
{
    public string word;
    public char[] KnownLetters = new char[Constants.wordLength];

    // +1 as the % operator used to access means a = 1, b=2 
    // this is fewer operations than correcting by 1 every array index access
    private byte[] KnownLetterCounts = new byte[Constants.validCharacters.Length + 1];
    public char[] MisplacedLetters = new char[Constants.wordLength];
    private byte[] MisplacedLetterCounts = new byte[Constants.validCharacters.Length + 1];
    public char[] EliminatedLetters = new char[Constants.wordLength];

    private Guess(string guessWord)
    {
        word = guessWord;
    }

    public int GetKnownLetterCount(char c)
    {
        return KnownLetterCounts[(int)c % 32];
    }

    private void IncreaseKnownLetterCount(char c)
    {
        KnownLetterCounts[(int)c % 32]++;
    }

    public int GetMisplacedLetterCount(char c)
    {
        return MisplacedLetterCounts[(int)c % 32];
    }

    private void IncreaseMisplacedLetterCount(char c)
    {
        MisplacedLetterCounts[(int)c % 32]++;
    }

    public static Guess FromTarget(string guessWord, string targetWord)
    {
        var guess = new Guess(guessWord);

        for (var i = 0; i < Constants.wordLength; i++)
        {
            var guessCharacter = guessWord[i];
            var targetCharacter = targetWord[i];
            if (guessCharacter == targetCharacter)
            {
                guess.KnownLetters[i] = guessCharacter;
                guess.IncreaseKnownLetterCount(guessCharacter);
            }
        }

        for (var i = 0; i < Constants.wordLength; i++)
        {
            var guessCharacter = guessWord[i];
            var targetWordCount = targetWord.Count(c => c == guessCharacter);
            if (targetWordCount > guess.GetKnownLetterCount(guessCharacter) + guess.GetMisplacedLetterCount(guessCharacter))
            {
                guess.MisplacedLetters[i] = guessCharacter;
                guess.IncreaseMisplacedLetterCount(guessCharacter);
            }
        }

        for (var i = 0; i < Constants.wordLength; i++)
        {
            var guessCharacter = guessWord[i];
            var letterFreq = guessWord.Count(c => c == guessCharacter);
            if (letterFreq > guess.GetKnownLetterCount(guessCharacter) + guess.GetMisplacedLetterCount(guessCharacter))
            {
                guess.EliminatedLetters[i] = guessCharacter;
            }
        }

        return guess;
    }

    public static Guess FromScore(string guessWord, string score)
    {
        var guess = new Guess(guessWord);
        for (int i = 0; i < Constants.wordLength; i++)
        {
            var scoreCharacter = score[i];
            var guessCharacter = guessWord[i];
            if (scoreCharacter == 'c')
            {
                guess.KnownLetters[i] = guessCharacter;
                guess.IncreaseKnownLetterCount(guessCharacter);
                continue;
            }
            if (scoreCharacter == 'm')
            {
                guess.MisplacedLetters[i] = guessCharacter;
                guess.IncreaseMisplacedLetterCount(guessCharacter);
                continue;
            }
            if (scoreCharacter == 'w')
            {
                guess.EliminatedLetters[i] = guessCharacter;
                continue;
            }
        }
        return guess;
    }

    public override string ToString()
    {
        var stringGuessRepresentation = new StringBuilder();
        for (int i = 0; i < Constants.wordLength; i++)
        {
            if (KnownLetters[i] != '\0')
            {
                stringGuessRepresentation.Append('c');
            }
            else if (MisplacedLetters[i] != '\0')
            {
                stringGuessRepresentation.Append('m');
                continue;
            }
            else
            {
                stringGuessRepresentation.Append('w');
            }
        }
        return $"{word} ({stringGuessRepresentation.ToString()})";
    }

}