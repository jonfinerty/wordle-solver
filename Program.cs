using System.Collections.Concurrent;

Console.WriteLine();
var validGuessingWords = ReadFile("wordlist_guesses.txt");
Console.WriteLine($"Number of valid guessing words = {validGuessingWords.Count}");

var validSolutionWords = ReadFile("wordlist_solutions.txt").Select(w => new Solution(w)).ToList();
Console.WriteLine($"Number of valid solution words = {validSolutionWords.Count}");

Console.WriteLine();
// FindOptimalFirstGuess(validGuessingWords, validSolutionWords);
// return;

while (validSolutionWords.Count > 1)
{
    var guess = ReadGuess();

    var watch = System.Diagnostics.Stopwatch.StartNew();
    validSolutionWords = computeRemainingPossibleAnswers(guess, validSolutionWords);
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
    var optimalGuessWordsInSolutionSet = optimalGuessWords.Where(w => validSolutionWords.Select(s => s.Word).Contains(w)).ToList();
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

static List<string> ReadFile(string fileName)
{
    var lines = File.ReadAllLines(fileName);
    return lines.ToList();
}

static Guess ReadGuess() {

    var guessInput = "";
    while(guessInput == null || !guessInput.All(char.IsLetter) || guessInput.Length != Constants.wordLength) {
        Console.WriteLine("Enter guess");
        guessInput = Console.ReadLine();
    }

    var scoreInput = "";
    var validInput = "cCmMwW";
    while(scoreInput == null || !scoreInput.All(c => validInput.Contains(c)) || scoreInput.Length != Constants.wordLength)
    {
        Console.WriteLine("Enter score, c=correct, m=misplaced, w=wrong. e.g. cmmwc");
        scoreInput = Console.ReadLine();
    }
    return Guess.FromScore(guessInput.ToLower(), scoreInput.ToLower());
}

// for guess choice, run through every target word
// assign the the guess choice a value based on how
// many words it removes from the remaining viable words
static List<string> FindOptimalGuessChoices(List<string> wordlist, List<Solution> remainingViableSolutionWords)
{
    int currentOptimalGuessValue = int.MaxValue;
    var resultsBag = new ConcurrentBag<(string, int)>();
    Parallel.ForEach(wordlist, new ParallelOptions { MaxDegreeOfParallelism = 24 }, possibleGuessWord =>
    {
        // foreach(var possibleGuessWord in wordlist)
        // {
        //LogDebug($"Guess word = {possibleGuessWord}");
        var totalPossibilities = 0;
        var aborted = false;
        foreach (var possibleSolutionWord in remainingViableSolutionWords)
        {
            //LogDebug($"Possible target word = {possibleSolutionWord}");
            var guess = Guess.FromTarget(possibleGuessWord, possibleSolutionWord.Word);
            var remainingPossibleAnswers = computeRemainingPossibleAnswers(guess, remainingViableSolutionWords);
            totalPossibilities += remainingPossibleAnswers.Count;
            if (totalPossibilities > currentOptimalGuessValue)
            {
                LogDebug($"Aborting guess '{possibleGuessWord}' as it's already suboptimal");
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
    //}

    var currentBestGuesses = new List<string>();
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

static List<Solution> computeRemainingPossibleAnswers(Guess guess, List<Solution> solutionWords)
{
    var validSolutions = new List<Solution>(solutionWords.Count);
    foreach (var solution in solutionWords)
    {

        bool validWord = true;

        for (var i = 0; i < Constants.wordLength; i++)
        {
            var knownLetter = guess.KnownLetters[i];
            if (knownLetter == '\0')
            {
                continue;
            }
            if (knownLetter != solution.Word[i])
            {
                //LogDebug($"Eliminating {word} from remainingViableWords as does not contain known letter {knownLetter} at index {i}");
                validWord = false;
                break;
            }
        }

        if (!validWord)
        {
            continue;
        }

        // Validate misplacedLetters counts
        for (int i = 0; i < Constants.wordLength; i++)
        {
            var letter = guess.MisplacedLetters[i];
            var countTarget = solution.GetLetterCount(letter);
            var knownCountGuesses = guess.GetKnownLetterCount(letter);
            var misplacedCountGuesses = guess.GetMisplacedLetterCount(letter);
            if (countTarget < knownCountGuesses + misplacedCountGuesses)
            {
                //LogDebug($"Eliminating {word} from remainingViableWords as does not contain enough misplaced letters '{letter}' ({countTarget} in word, {knownCountGuesses} in known locations, {misplacedCountGuesses} in unknown locations");
                validWord = false;
                break;
            }

            // Validate not in known bad locations
            if (letter == solution.Word[i])
            {
                //LogDebug($"Eliminating {word} from remainingViableWords as contains letter '{letter}' in known-misplaced position '{i}'");
                validWord = false;
                break;
            }
        }

        if (!validWord)
        {
            continue;
        }

        for (int i = 0; i < Constants.wordLength; i++)
        {
            var eliminatedLetter = guess.EliminatedLetters[i];

            if (eliminatedLetter == '\0')
            {
                continue;
            }
            // if number of letter in word > known + misplaced then non viable
            var wordOccurances = solution.GetLetterCount(eliminatedLetter);
            var knownOccurances = guess.GetKnownLetterCount(eliminatedLetter);
            var misplacedOccurances = guess.GetMisplacedLetterCount(eliminatedLetter);
            if (wordOccurances > knownOccurances + guess.GetMisplacedLetterCount(eliminatedLetter))
            {
                //LogDebug($"Eliminating {word} from remainingViableWords as contains eliminated letter '{eliminatedLetter}' ({wordOccurances} times in word, {knownOccurances} times in known locations, {misplacedOccurances} times misplaced)");
                validWord = false;
                break;
            }
        }

        if (validWord)
        {
            //LogDebug($"Adding '{word}' as valid word for guess = {guess}");
            validSolutions.Add(solution);
        }
    }

    return validSolutions;
}

#pragma warning disable CS8321
static void FindOptimalFirstGuess(List<string> validGuessingWords, List<Solution> validSolutionWords)
{
    var timer = System.Diagnostics.Stopwatch.StartNew();
    var firstGuessWords = FindOptimalGuessChoices(validGuessingWords, validSolutionWords);
    if (firstGuessWords.Count > 1)
    {
        firstGuessWords = firstGuessWords.Where(w => validSolutionWords.Any(s => s.Word == w)).ToList();
    }
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