using System.Collections.Concurrent;

Console.WriteLine();
var validGuessingWords = File.ReadAllLines("wordlist_guesses.txt").Select(w => new Word(w)).ToList();
var validSolutionWords = File.ReadAllLines("wordlist_solutions.txt").Select(w => new Word(w)).ToList();

//FindOptimalFirstGuess(validGuessingWords, validSolutionWords);

while (validSolutionWords.Count > 1)
{
    var guess = ReadGuess();

    var watch = System.Diagnostics.Stopwatch.StartNew();
    validSolutionWords = getRemainingPossibleSolutions(guess, validSolutionWords).ToList();
    Console.WriteLine($"Remaining valid solution words: {validSolutionWords.Count}");
    if (validSolutionWords.Count == 0)
    {
        Console.WriteLine("Looks like the word you're searching for isn't in the target dictionary, :(");
        return;
    }

    if (validSolutionWords.Count == 1)
    {
        Console.WriteLine($"Only one possibility remains, and so the answer is {validSolutionWords.First()}!");
        return;
    }

    if (validSolutionWords.Count < 20)
    {
        Console.WriteLine(string.Join(',', validSolutionWords));
    }

    var optimalGuessWords = FindOptimalGuessChoices(validGuessingWords, validSolutionWords);
    // if chosing a guess from either the validGuessingWords or remainingViableWords is
    // equally optimal, favour a remainingViableWord as you might just get lucky
    var optimalGuessWordsInSolutionSet = optimalGuessWords.Where(w => validSolutionWords.Contains(w)).ToList();
    watch.Stop();
    var elapsedMs = watch.ElapsedMilliseconds;
    if (optimalGuessWordsInSolutionSet.Count > 0)
    {
        Console.WriteLine($"Suggested word choices: {string.Join(',', optimalGuessWordsInSolutionSet)}, determined in {elapsedMs}ms");
    }
    else
    {
        Console.WriteLine($"Suggested word choices: {string.Join(',', optimalGuessWords)}, determined in {elapsedMs}ms");
    }
}

static ScoredGuess ReadGuess()
{
    var guessInput = "";
    while (guessInput == null || !guessInput.All(char.IsLetter) || guessInput.Length != Constants.wordLength)
    {
        Console.WriteLine("Enter guess");
        guessInput = Console.ReadLine();
    }

    var scoreInput = "";
    var validInput = "cCmMwW";
    while (scoreInput == null || !scoreInput.All(c => validInput.Contains(c)) || scoreInput.Length != Constants.wordLength)
    {
        Console.WriteLine("Enter score, c=correct, m=misplaced, w=wrong. e.g. cmmwc");
        scoreInput = Console.ReadLine();
    }

    var guess = new Word(guessInput.ToLower());

    return ScoredGuess.FromScore(guess, scoreInput.ToLower());
}

// for guess choice, run through every target word
// assign the the guess choice a value based on how
// many words it removes from the remaining viable words
static IEnumerable<Word> FindOptimalGuessChoices(IEnumerable<Word> wordlist, IEnumerable<Word> remainingViableSolutionWords)
{
    int currentOptimalGuessValue = int.MaxValue;
    var resultsBag = new ConcurrentBag<(Word, int)>();
    Parallel.ForEach(wordlist, possibleGuessWord =>
    {
        LogDebug($"Guess word = {possibleGuessWord}");
        var totalPossibilities = 0;
        var aborted = false;
        foreach (var possibleSolutionWord in remainingViableSolutionWords)
        {
            var scoredGuess = ScoredGuess.FromSolution(possibleGuessWord, possibleSolutionWord);
            var remainingPossibleAnswers = countRemainingPossibleSolutions(scoredGuess, remainingViableSolutionWords);
            totalPossibilities += remainingPossibleAnswers;
            if (totalPossibilities > currentOptimalGuessValue)
            {
                //LogDebug($"Aborting guess '{possibleGuessWord}' as it's already suboptimal");
                aborted = true;
                break;
            }
        }

        if (totalPossibilities > 0 && !aborted)
        {
            //LogDebug($"Guess '{possibleGuessWord}' evaluated with score '{totalPossibilities}'");
            resultsBag.Add((possibleGuessWord, totalPossibilities));
            if (totalPossibilities < currentOptimalGuessValue)
            {
                currentOptimalGuessValue = totalPossibilities;
            }
        }
    });


    var currentBestGuesses = new List<Word>();
    var currentBestGuessValue = int.MaxValue;
    foreach (var evalutedGuess in resultsBag)
    {
        if (evalutedGuess.Item2 < currentBestGuessValue && evalutedGuess.Item2 > 0)
        {
            currentBestGuessValue = evalutedGuess.Item2;
            currentBestGuesses.Clear();
            currentBestGuesses.Add(evalutedGuess.Item1);
            LogDebug($"New optimal guess found: {evalutedGuess.Item1}, which reduces the set of possible answers to {evalutedGuess.Item2} across all games");
        }
        else if (evalutedGuess.Item2 == currentBestGuessValue)
        {
            currentBestGuesses.Add(evalutedGuess.Item1);
            LogDebug($"New equally optimal guess found: {evalutedGuess.Item1}, which reduces the set of possible answers to {evalutedGuess.Item2} across all games");
        }
    }

    return currentBestGuesses;
}

static IEnumerable<Word> getRemainingPossibleSolutions(ScoredGuess scoredGuess, IEnumerable<Word> solutions)
{
    return solutions.Where(s => isValidSolution(scoredGuess, s));
}

static int countRemainingPossibleSolutions(ScoredGuess scoredGuess, IEnumerable<Word> solutions)
{
    return solutions.Count(s => isValidSolution(scoredGuess, s));
}

static bool isValidSolution(ScoredGuess guess, Word solution)
{
    for (var i = 0; i < Constants.wordLength; i++)
    {
        var knownLetter = guess.KnownLetters[i];
        if (knownLetter != '\0')
        {
            if (knownLetter == solution.Letters[i])
            {
                continue;
            }
            else
            {
                return false;
            }
        }

        var misplacedLetter = guess.MisplacedLetters[i];
        if (misplacedLetter != '\0')
        {
            var countTarget = solution.GetLetterCount(misplacedLetter);
            var knownCountGuesses = guess.GetKnownLetterCount(misplacedLetter);
            var misplacedCountGuesses = guess.GetMisplacedLetterCount(misplacedLetter);
            if (countTarget < knownCountGuesses + misplacedCountGuesses)
            {
                return false;
            }

            // Validate not in known bad locations
            if (misplacedLetter == solution.Letters[i])
            {
                return false;
            }

            continue;
        }

        var eliminatedLetter = guess.EliminatedLetters[i];

        // if it's not known, or misplaced then it must be eliminated, so no need to check != '\0'
        // if number of letter in word > known + misplaced then non viable
        var wordOccurances = solution.GetLetterCount(eliminatedLetter);
        var knownOccurances = guess.GetKnownLetterCount(eliminatedLetter);
        var misplacedOccurances = guess.GetMisplacedLetterCount(eliminatedLetter);
        if (wordOccurances > knownOccurances + guess.GetMisplacedLetterCount(eliminatedLetter))
        {
            return false;
        }
    }

    return true;
}

#pragma warning disable CS8321
static void FindOptimalFirstGuess(IEnumerable<Word> validGuessingWords, IEnumerable<Word> validSolutionWords)
{
    var timer = System.Diagnostics.Stopwatch.StartNew();
    var firstGuessWords = FindOptimalGuessChoices(validGuessingWords, validSolutionWords);
    timer.Stop();
    var elapsedTime = timer.ElapsedMilliseconds;
    Console.WriteLine($"Suggested word choices: {string.Join(',', firstGuessWords)}, determined in {elapsedTime}ms");
    return;
}

static void LogDebug(string log)
{
    if (Constants.debug)
    {
#pragma warning disable CS0162
        Console.WriteLine(log);
    }
}

static class Constants
{
    public const bool debug = false;
    public const string validCharacters = "abcdefghijklmnopqrstuvwxyz";
    public const int wordLength = 5;
}