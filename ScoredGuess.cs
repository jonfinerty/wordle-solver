using System.Text;

public class ScoredGuess
{
    public Word Guess;
    public char[] KnownLetters = new char[Constants.wordLength];
    // +1 as the % operator used to access means a = 1, b=2 
    // this is fewer operations than correcting by 1 every array index access
    public byte[] KnownLetterCounts = new byte[Constants.validCharacters.Length + 1];
    public char[] MisplacedLetters = new char[Constants.wordLength];
    public byte[] MisplacedLetterCounts = new byte[Constants.validCharacters.Length + 1];
    public char[] EliminatedLetters = new char[Constants.wordLength];

    private ScoredGuess(Word guess)
    {
        Guess = guess;
    }

    private void IncreaseKnownLetterCount(char c)
    {
        KnownLetterCounts[(int)c % 32]++;
    }

    private void IncreaseMisplacedLetterCount(char c)
    {
        MisplacedLetterCounts[(int)c % 32]++;
    }

    public static ScoredGuess FromSolution(Word guess, Word solution)
    {
        var scoredGuess = new ScoredGuess(guess);

        for (var i = 0; i < Constants.wordLength; i++)
        {
            var guessCharacter = guess.Letters[i];
            var targetCharacter = solution.Letters[i];
            if (guessCharacter == targetCharacter)
            {
                scoredGuess.KnownLetters[i] = guessCharacter;
                scoredGuess.IncreaseKnownLetterCount(guessCharacter);
            }
        }

        for (var i = 0; i < Constants.wordLength; i++)
        {
            var guessCharacter = guess.Letters[i];
            var targetWordCount = solution.LetterCounts[guessCharacter % 32];
            if (targetWordCount > scoredGuess.KnownLetterCounts[guessCharacter % 32] + scoredGuess.MisplacedLetterCounts[guessCharacter % 32])
            {
                scoredGuess.MisplacedLetters[i] = guessCharacter;
                scoredGuess.IncreaseMisplacedLetterCount(guessCharacter);
            }
        }

        for (var i = 0; i < Constants.wordLength; i++)
        {
            var guessCharacter = guess.Letters[i];
            var letterFreq = guess.LetterCounts[guessCharacter % 32];
            if (letterFreq > scoredGuess.KnownLetterCounts[guessCharacter % 32] + scoredGuess.MisplacedLetterCounts[guessCharacter% 32])
            {
                scoredGuess.EliminatedLetters[i] = guessCharacter;
            }
        }

        return scoredGuess;
    }

    public static ScoredGuess FromScore(Word guess, string score)
    {
        var scoredGuess = new ScoredGuess(guess);
        for (int i = 0; i < Constants.wordLength; i++)
        {
            var scoreCharacter = score[i];
            var guessCharacter = guess.Letters[i];
            if (scoreCharacter == 'c')
            {
                scoredGuess.KnownLetters[i] = guessCharacter;
                scoredGuess.IncreaseKnownLetterCount(guessCharacter);
            }
            else if (scoreCharacter == 'm')
            {
                scoredGuess.MisplacedLetters[i] = guessCharacter;
                scoredGuess.IncreaseMisplacedLetterCount(guessCharacter);
            }
            else // therefore scoreCharacter == 'w'
            {
                scoredGuess.EliminatedLetters[i] = guessCharacter;
            }
        }
        return scoredGuess;
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
        return $"{Guess} ({stringGuessRepresentation.ToString()})";
    }

}