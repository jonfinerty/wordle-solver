using System.Text;
using System.Runtime.CompilerServices;

public class Guess
{
    private static string validCharacters = "abcdefghijklmnopqrstuvwxyz";
    public string word;
    public char[] KnownLetters;

    // +1 as the % operator used to access means a = 1, b=2 
    // this is fewer operations than correcting by 1 every array index access
    private int[] KnownLetterCounts = new int[validCharacters.Length + 1]; 
    public char[] MisplacedLetters;
    private int[] MisplacedLetterCounts = new int[validCharacters.Length + 1];
    public char[] EliminatedLetters;

    private Guess(string guessWord)
    {
        word = guessWord;
        KnownLetters = new char[word.Length];
        MisplacedLetters = new char[word.Length];
        EliminatedLetters = new char[word.Length];
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

        for (var i = 0; i < guessWord.Length; i++)
        {
            var guessCharacter = guessWord[i];
            var targetCharacter = targetWord[i];
            if (guessCharacter == targetCharacter)
            {
                guess.KnownLetters[i] = guessCharacter;
                guess.IncreaseKnownLetterCount(guessCharacter);
            }
        }

        for (var i = 0; i < guessWord.Length; i++)
        {
            var guessCharacter = guessWord[i];
            var targetWordCount = targetWord.Count(c => c == guessCharacter);
            if (targetWordCount > guess.GetKnownLetterCount(guessCharacter) + guess.GetMisplacedLetterCount(guessCharacter))
            {
                guess.MisplacedLetters[i] = guessCharacter;
                guess.IncreaseMisplacedLetterCount(guessCharacter);
            }
        }

        for (var i = 0; i < guessWord.Length; i++)
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
        for (int i = 0; i < score.Length; i++)
        {
            var scoreCharacter = score[i];
            var guessCharacter = guessWord[i];
            if (Char.ToUpper(scoreCharacter) == 'C')
            {
                guess.KnownLetters[i] = guessCharacter;
                guess.IncreaseKnownLetterCount(guessCharacter);
                continue;
            }
            if (Char.ToUpper(scoreCharacter) == 'M')
            {
                guess.MisplacedLetters[i] = guessCharacter;
                guess.IncreaseMisplacedLetterCount(guessCharacter);
                continue;
            }
            if (Char.ToUpper(scoreCharacter) == 'W')
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
        for (int i = 0; i < word.Length; i++)
        {
            if (KnownLetters[i] != '\0')
            {
                stringGuessRepresentation.Append('C');
            } 
            else if (MisplacedLetters[i] != '\0')
            {
                stringGuessRepresentation.Append('M');
                continue;
            }
            else 
            {
                stringGuessRepresentation.Append('W');
            }
        }
        return $"{word} ({stringGuessRepresentation.ToString()})";
    }

}