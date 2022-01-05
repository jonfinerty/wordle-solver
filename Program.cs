using System.Collections.Concurrent;

Console.WriteLine();
var validGuessingWords = ReadFile("wordlist_guesses.txt");
Console.WriteLine($"Number of valid guessing words = {validGuessingWords.Count}");

var validSolutionWords = ReadFile("wordlist_solutions.txt");
Console.WriteLine($"Number of valid solution words = {validSolutionWords.Count}");

Console.WriteLine();
// Console.WriteLine("Enter word length");
// var input = Console.ReadLine();
// if (input == null) {
//     return;
// }
// var wordLength = Int32.Parse(input);
//var sanitisedWords = SanitiseWords(allGuesses, wordLength);
//Console.WriteLine($"Number of {wordLength} letter words = {sanitisedWords.Count}");
var remainingViableWords = validSolutionWords;
Console.WriteLine($"Remaining possibilities: {remainingViableWords.Count}");

// var timer = System.Diagnostics.Stopwatch.StartNew();
// var firstGuessWords = FindOptimalGuessChoices(validGuessingWords, remainingViableWords);
// if (firstGuessWords.Count > 1) {
//     firstGuessWords = firstGuessWords.Where(w => remainingViableWords.Contains(w)).ToList();
// }
// timer.Stop();
// var elapsedTime = timer.ElapsedMilliseconds;
// Console.WriteLine($"Suggested word choices: {string.Join(',',firstGuessWords)}, determined in {elapsedTime}ms");
// return;

while (true)
{
    Console.WriteLine("Enter guess");
    var input = Console.ReadLine();
    if (input == null)
    {
        return;
    }
    Console.WriteLine("Enter score, C=correct, M=misplaced, W=wrong");
    var manualScore = Console.ReadLine();
    if (manualScore == null)
    {
        return;
    }
    var guess = Guess.FromScore(input.ToLower(), manualScore);

    remainingViableWords = computeRemainingPossibleAnswers(guess, remainingViableWords);
    Console.WriteLine($"Remaining possibilities: {remainingViableWords.Count}");
    if (remainingViableWords.Count == 0)
    {
        Console.WriteLine("Looks like the word you're searching for isn't in the target dictionary, :(");
        return;
    }

    if (remainingViableWords.Count == 1)
    {
        Console.WriteLine($"Only one possibility remains, and so the answer is {remainingViableWords.First()}!");
        return;
    }

    if (remainingViableWords.Count < 20)
    {
        Console.WriteLine(string.Join(',', remainingViableWords));
    }

    var watch = System.Diagnostics.Stopwatch.StartNew();
    var recommendedWords = FindOptimalGuessChoices(validGuessingWords, remainingViableWords);
    var recommendedWordsInAnswerSet = recommendedWords.Where(w => remainingViableWords.Contains(w)).ToList();
    //Console.WriteLine($"Unfiltered suggested word choices: {string.Join(',', recommendedWords)}");
    watch.Stop();
    var elapsedMs = watch.ElapsedMilliseconds;
    if (recommendedWordsInAnswerSet.Count > 0)
    {
        Console.WriteLine($"Suggested word choices: {string.Join(',', recommendedWordsInAnswerSet)}, determined in {elapsedMs}ms");
    }
    else
    {
        Console.WriteLine($"Suggested word choices: {string.Join(',', recommendedWords)}, determined in {elapsedMs}ms");
    }
}

static List<string> ReadFile(string fileName)
{
    var lines = File.ReadAllLines(fileName);
    return lines.ToList();
}


// for guess choice, run through every target word
// assign the the guess choice a value based on how many words it removes from the corpus
static List<string> FindOptimalGuessChoices(List<string> wordlist, List<string> remainingViableWords)
{

    var currentOptimalGuess = int.MaxValue;
    var results = new ConcurrentBag<(string, int)>();
    Parallel.ForEach(wordlist, new ParallelOptions { MaxDegreeOfParallelism = 12 }, possibleGuessWord =>
    {
        // foreach (var possibleGuessWord in wordlist)
        // {
        //Console.WriteLine($"Guess word = {possibleGuessWord}");
        var totalPossibilities = 0;
        var aborted = false;
        foreach (var possibleTargetWord in remainingViableWords)
        {
            //Console.WriteLine($"Possible target word = {possibleTargetWord}");
            var guess = Guess.FromTarget(possibleGuessWord, possibleTargetWord);
            var remainingPossibleAnswers = computeRemainingPossibleAnswers(guess, remainingViableWords);
            totalPossibilities += remainingPossibleAnswers.Count;
            if (totalPossibilities > currentOptimalGuess)
            {
                //Console.WriteLine($"Aborting guess '{possibleGuessWord}' as it's already suboptimal");
                aborted = true;
                break;
            }
        }

        if (totalPossibilities > 0 && !aborted)
        {
            //Console.WriteLine($"Guess '{possibleGuessWord}' evaluated with score '{totalPossibilities}'");
            results.Add((possibleGuessWord, totalPossibilities));
            if (totalPossibilities < currentOptimalGuess)
            {
                currentOptimalGuess = totalPossibilities;
            }
        }
    });
    //}

    var currentBestGuesses = new List<string>();
    var currentBestGuessTotalPossibilities = int.MaxValue;
    foreach (var evalutedGuess in results)
    {
        if (evalutedGuess.Item2 < currentBestGuessTotalPossibilities && evalutedGuess.Item2 > 0)
        {
            currentBestGuessTotalPossibilities = evalutedGuess.Item2;
            currentBestGuesses.Clear();
            currentBestGuesses.Add(evalutedGuess.Item1);
            //Console.WriteLine($"New optimal guess found: {evalutedGuess.Item1}, which reduces the set of possible answers to {evalutedGuess.Item2} across all games");
        }
        else if (evalutedGuess.Item2 == currentBestGuessTotalPossibilities)
        {
            currentBestGuesses.Add(evalutedGuess.Item1);
            //Console.WriteLine($"New equally optimal guess found: {evalutedGuess.Item1}, which reduces the set of possible answers to {evalutedGuess.Item2} across all games");
        }
    }

    return currentBestGuesses;
}

static List<string> computeRemainingPossibleAnswers(Guess guess, List<string> remainingViableWords)
{
    var validWords = new List<string>(remainingViableWords.Count);
    foreach (var word in remainingViableWords)
    {

        bool validWord = true;

        for (var i = 0; i < guess.KnownLetters.Length; i++)
        {
            var knownLetter = guess.KnownLetters[i];
            if (knownLetter == '\0')
            {
                continue;
            }
            if (knownLetter != word[i])
            {
                //Console.WriteLine($"Eliminating {word} from remainingViableWords as does not contain known letter {item.Value} at index {item.Key}");
                validWord = false;
                break;
            }
        }

        if (!validWord)
        {
            continue;
        }

        // Validate misplacedLetters counts
        for (int i = 0; i < guess.MisplacedLetters.Length; i++)
        {
            var letter = guess.MisplacedLetters[i];
            var countTarget = word.Count(c => c == letter);
            if (countTarget < guess.GetKnownLetterCount(letter) + guess.GetMisplacedLetterCount(letter))
            {
                //Console.WriteLine($"Eliminating {word} from remainingViableWords as does not contain enough misplaced letters '{letter}' ({countTarget} in word, {knownCountGuess} in known locations, {misplacedCountGuess} in unknown locations");
                validWord = false;
                break;
            }

            // Validate not in known bad locations
            if (letter == word[i])
            {
                //Console.WriteLine($"Eliminating {word} from remainingViableWords as contains letter '{item.Value}' in known-misplaced position '{item.Key}'");
                validWord = false;
                break;
            }
        }

        if (!validWord)
        {
            continue;
        }

        foreach (var eliminatedLetter in guess.EliminatedLetters)
        {
            if (eliminatedLetter == '\0')
            {
                continue;
            }
            // if number of letter in word > known + misplaced then non viable
            var wordOccurances = word.Count(c => c == eliminatedLetter);

            if (wordOccurances > guess.GetKnownLetterCount(eliminatedLetter) + guess.GetMisplacedLetterCount(eliminatedLetter))
            {
                //Console.WriteLine($"Eliminating {word} from remainingViableWords as contains eliminated letter '{eliminatedLetter}' ({wordOccurances} times in word, {knownOccurances} times in known locations, {misplacedOccurances} times misplaced)");
                validWord = false;
                break;
            }
        }

        if (validWord)
        {
            //Console.WriteLine($"Adding '{word}' as valid word for guess = {guess}");
            validWords.Add(word);
        }
    }

    return validWords;
}


