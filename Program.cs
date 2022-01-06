using System.Collections.Concurrent;

Console.WriteLine();
var validGuessingWords = File.ReadAllLines("wordlist_guess_words.txt").Select(w => new Word(w)).ToList();
var validSolutionWords = File.ReadAllLines("wordlist_solution_words.txt").Select(w => new Word(w)).ToList();

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

    var optimalGuessWords = FindOptimalGuessChoices2(validGuessingWords, validSolutionWords);
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
    var validScoreInput = "cCmMwW";
    var guessInput = "";
    while (guessInput == null || !guessInput.All(char.IsLetter) || guessInput.Length != Constants.wordLength || guessInput.All(c => validScoreInput.Contains(c)))
    {
        Console.WriteLine("Enter guess");
        guessInput = Console.ReadLine();
    }

    var scoreInput = "";
    while (scoreInput == null || !scoreInput.All(c => validScoreInput.Contains(c)) || scoreInput.Length != Constants.wordLength)
    {
        Console.WriteLine("Enter score, c=correct, m=misplaced, w=wrong. e.g. cmmwc");
        scoreInput = Console.ReadLine();
    }

    var guess = new Word(guessInput.ToLower());

    return ScoredGuess.FromScore(guess, scoreInput.ToLower());
}

// for guess choice, run through every target word
// assign the the guess choice a value based on the
// expected number of guesses
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

// todo don't need both loops, should be decimal
static double getExpectedNumberOfGuesses(Word guessWord, IEnumerable<Word> guessWordList, List<Word> remainingViableSolutionWords, int guessDepth)
{
    if (remainingViableSolutionWords.Count == 1) {
        return 0;
    }

    if (guessDepth >= Constants.numberOfGuesses) {
        return 0;
    }
    
    var totalGuesses = 0.0;
    LogDebug($"Guess word = {guessWord}");
    foreach (var possibleSolutionWord in remainingViableSolutionWords)
    {
        var scoredGuess = ScoredGuess.FromSolution(guessWord, possibleSolutionWord);
        var remainingPossibleAnswers = getRemainingPossibleSolutions(scoredGuess, remainingViableSolutionWords);
        foreach (var nextGuessWord in guessWordList) {
            totalGuesses = getExpectedNumberOfGuesses(nextGuessWord, guessWordList, remainingPossibleAnswers.ToList(), guessDepth+1) + 1;
        }
    }

    return totalGuesses / (double) remainingViableSolutionWords.Count();    
}


static IEnumerable<Word> FindOptimalGuessChoices2(IEnumerable<Word> guessWordlist, IEnumerable<Word> remainingViableSolutionWords) {
    var currentOptimalGuessValue = double.MaxValue;
    var resultsBag = new ConcurrentBag<(Word, double)>();
    
    // Parallel.ForEach(guessWordlist, possibleGuessWord =>
    // {
    foreach(var possibleGuessWord in guessWordlist)
    {
        LogDebug($"Guess word = {possibleGuessWord}");
        var expectedNumberOfGuesses = getExpectedNumberOfGuesses(possibleGuessWord, guessWordlist, remainingViableSolutionWords.ToList(), 0);
        
        if (expectedNumberOfGuesses <= currentOptimalGuessValue)
        {
            resultsBag.Add((possibleGuessWord, expectedNumberOfGuesses));
            if (expectedNumberOfGuesses < currentOptimalGuessValue)
            {
                currentOptimalGuessValue = expectedNumberOfGuesses;
            }
        }
    //});
    }

    var currentBestGuesses = new List<Word>();
    var currentBestGuessValue = double.MaxValue;
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
            var misplacedLetterIndex = misplacedLetter % 32;
            var countTarget = solution.LetterCounts[misplacedLetterIndex];
            var knownCountGuesses = guess.KnownLetterCounts[misplacedLetterIndex];
            var misplacedCountGuesses = guess.MisplacedLetterCounts[misplacedLetterIndex];
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
        var eliminatedLetterIndex = eliminatedLetter % 32;
        var wordOccurances = solution.LetterCounts[eliminatedLetterIndex];
        var knownOccurances = guess.KnownLetterCounts[eliminatedLetterIndex];
        var misplacedOccurances = guess.MisplacedLetterCounts[eliminatedLetterIndex];
        if (wordOccurances > knownOccurances + misplacedOccurances)
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