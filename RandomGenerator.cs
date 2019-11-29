using System;

public class RandomGenerator {
    private static Random _random = new Random();

    public static int Next(int max) {
        return _random.Next(max);
    }



}