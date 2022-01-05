using System.Text;

public class ScoredGuess
{
    public Word Guess;
    public char[] KnownLetters = new char[Constants.wordLength];
    // +1 as the % operator used to access means a = 1, b=2 
    // this is fewer operations than correcting by 1 every array index access
    private byte[] KnownLetterCounts = new byte[Constants.validCharacters.Length + 1];
    public char[] MisplacedLetters = new char[Constants.wordLength];
    private byte[] MisplacedLetterCounts = new byte[Constants.validCharacters.Length + 1];
    public char[] EliminatedLetters = new char[Constants.wordLength];

    private ScoredGuess(Word guess)
    {
        Guess = guess;
    }

    public int GetKnownLetterCount(int offsetLetter)
    {
        return KnownLetterCounts[offsetLetter];
    }

    private void IncreaseKnownLetterCount(int offsetLetter)
    {
        KnownLetterCounts[offsetLetter]++;
    }

    public int GetMisplacedLetterCount(int offsetLetter)
    {
        return MisplacedLetterCounts[offsetLetter];
    }

    private void IncreaseMisplacedLetterCount(int offsetLetter)
    {
        MisplacedLetterCounts[offsetLetter]++;
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
                var guessCharacterOffset = guess.offsetLetters[i];
                scoredGuess.IncreaseKnownLetterCount(guessCharacterOffset);
            }
        }

        for (var i = 0; i < Constants.wordLength; i++)
        {
            var guessLetterOffset = guess.offsetLetters[i];
            var targetWordCount = solution.GetLetterCount(guessLetterOffset);
            if (targetWordCount > scoredGuess.GetKnownLetterCount(guessLetterOffset) + scoredGuess.GetMisplacedLetterCount(guessLetterOffset))
            {
                scoredGuess.MisplacedLetters[i] = guess.Letters[i];
                scoredGuess.IncreaseMisplacedLetterCount(guessLetterOffset);
            }
        }

        for (var i = 0; i < Constants.wordLength; i++)
        {
            var guessCharacter = guess.Letters[i];
            var guessLetterOffset = guess.offsetLetters[i];
            var letterFreq = guess.GetLetterCount(guessLetterOffset);
            if (letterFreq > scoredGuess.GetKnownLetterCount(guessLetterOffset) + scoredGuess.GetMisplacedLetterCount(guessLetterOffset))
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