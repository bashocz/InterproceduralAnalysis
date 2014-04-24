using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterproceduralAnalysis
{
    class Program
    {
        private static string programFile;
        private static bool printLA;
        private static bool printSA;
        private static bool printIAM;
        private static bool printIAG;
        private static bool printIALE;

        private static int w;

        static int Main(string[] args)
        {
            programFile = null;
            printLA = false;
            printSA = false;
            printIAM = false;
            printIAG = false;
            printIALE = false;

            if (!ParseArguments(args))
            {
                PrintHelp();
                Console.ReadKey();
                return -1;
            }
            
            LexicalAnalyzer la = new LexicalAnalyzer(programFile, printLA);
            SyntacticAnalyzer sa = new SyntacticAnalyzer(la);
            ProgramAst prg;
            if (!sa.GetAST(out prg))
            {
                Console.ReadKey();
                return -1;
            }

            SemanticAnalyzer sea = new SemanticAnalyzer();
            if (!sea.CheckAST(prg))
            {
                Console.ReadKey();
                return -1;
            }

            Console.WriteLine("Pocet promennych: {0}, pocet funkci: {1}", prg.VarsDecl.Count, prg.OrigFncs.Count);

            StatementConverter sc = new StatementConverter(printSA);
            sc.ConvertToIfGoto(prg);

            GraphGenerator gg = new GraphGenerator();
            gg.CreateGraph(prg);

            Stopwatch s = new Stopwatch();
            s.Start();

            int n = prg.VarsDecl.Count;
            InterproceduralAnalyzer ia = new InterproceduralAnalyzer(w, n, printIAM, printIAG, printIALE);
            ia.Analyze(prg);

            s.Stop();

            WriteProgram wp = new WriteProgram();
            wp.Write(prg, programFile);

            Console.WriteLine("Cas analyzy: {0:N3} s", s.ElapsedMilliseconds / 1000.0);
            Console.WriteLine("Stiskni klavesu...");
            Console.ReadKey();
            return 0;
        }

        static bool ParseArguments(string[] args)
        {
            int qi = Array.FindIndex(args, x => x == "/?");
            if (qi >= 0)
            {
                return false;
            }

            int ai = 0;
            bool isProgram = false, isW = false;

            while (ai < args.Length)
            {
                if (args[ai] == "/p")
                {
                    if (isProgram)
                    {
                        Console.WriteLine("Parametr jmeno souboru programu musi byt pouze jednou.");
                        Console.WriteLine();
                        return false;
                    }

                    if (ai >= (args.Count() - 1))
                    {
                        Console.WriteLine("Parametr jmeno souboru programu je povinny.");
                        Console.WriteLine();
                        return false;
                    }

                    programFile = args[ai + 1];
                    isProgram = true;

                    ai += 2;
                    continue;
                }

                if (args[ai] == "/w")
                {
                    if (isW)
                    {
                        Console.WriteLine("Parametr rozsah celocislenych promennych w musi byt pouze jednou.");
                        Console.WriteLine();
                        return false;
                    }

                    if (ai >= (args.Count() - 1))
                    {
                        Console.WriteLine("Parametr rozsah celocislenych promennych w je povinny.");
                        Console.WriteLine();
                        return false;
                    }

                    int wInt;
                    if (!int.TryParse(args[ai + 1], out wInt))
                    {
                        Console.WriteLine("Parametr rozsah celocislenych promennych w='{0}' neni celociselna hodnota.", args[ai + 1]);
                        Console.WriteLine();
                        return false;
                    }
                    if ((wInt < 1) && (wInt > 32))
                    {
                        Console.WriteLine("Parametr rozsah celocislenych promennych w='{0}' musi byt v rozsahu <1,32>.", wInt);
                        Console.WriteLine();
                        return false;
                    }

                    w = wInt;
                    isW = true;

                    ai += 2;
                    continue;
                }

                if (args[ai] == "/d")
                {
                    if (ai >= (args.Count() - 1))
                    {
                        Console.WriteLine("Volitelny parametr zobrazeni musi specifikovat volby.");
                        Console.WriteLine();
                        return false;
                    }

                    string[] dos = args[ai + 1].Split('+');
                    foreach (string doi in dos)
                    {
                        switch (doi)
                        {
                            case "la":
                                printLA = true;
                                break;
                            case "sa":
                                printSA = true;
                                break;
                            case "iam":
                                printIAM = true;
                                break;
                            case "iag":
                                printIAG = true;
                                break;
                            case "iale":
                                printIALE = true;
                                break;
                            case "all":
                                printLA = true;
                                printSA = true;
                                printIAM = true;
                                printIAG = true;
                                printIALE = true;
                                break;
                            default:
                                Console.WriteLine("Neznama volba zobrazeni '{0}'", doi);
                                Console.WriteLine();
                                return false;
                        }
                    }

                    ai += 2;
                    continue;
                }

                Console.WriteLine("Neznamy parametr '{0}'.", args[ai]);
                Console.WriteLine();
                return false;
            }

            if (!isProgram)
            {
                Console.WriteLine("Parametr jmeno souboru programu je povinny.");
                Console.WriteLine();
                return false;
            }

            if (!File.Exists(programFile))
            {
                Console.WriteLine(string.Format("Soubor programu '{0}' neexistuje.", programFile));
                Console.WriteLine();
                return false;
            }

            if (!isW)
            {
                Console.WriteLine("Parametr rozsah celocislenych promennych w je povinny.");
                Console.WriteLine();
                return false;
            }

            return true;
        }

        static void PrintHelp()
        {
            Console.WriteLine();
            Console.WriteLine("Pouziti:");
            Console.WriteLine("    intprocan.exe /p <jmeno_programu> /w <rozsah_int> [ /? |");
            Console.WriteLine("                                                       /d [all+la+sa+iam+iag+iale]");
            Console.WriteLine();
            Console.WriteLine("Kde:");
            Console.WriteLine("    /p <jmeno_programu>   povinny parametr - jmeno programu");
            Console.WriteLine("    /w <rozsah_int>       povinny parametr - je rozsah celociselnych promennych 2^w, kde w = <1,64)");
            Console.WriteLine("Volitelne:");
            Console.WriteLine("    /?                    zobrazeni napovedy");
            Console.WriteLine("    /d                    zobrazeni pomocnych vypisu");
            Console.WriteLine("    /d all                zobrazeni vsech pomocnych vypisu");
            Console.WriteLine("    /d la                 zobrazeni pomocnych vypisu lexikalni analyzy");
            Console.WriteLine("    /d sa                 zobrazeni pomocnych vypisu prevodu AST na podminku a goto");
            Console.WriteLine("    /d iam                zobrazeni pomocnych vypisu interproceduralni analyzy vypoctu zmenovych matic");
            Console.WriteLine("    /d iag                zobrazeni pomocnych vypisu interproceduralni analyzy vypoctu generatoru");
            Console.WriteLine("    /d iale               zobrazeni pomocnych vypisu interproceduralni analyzy vypoctu linearnich rovnic");
            Console.WriteLine();
            Console.WriteLine("Priklady:");
            Console.WriteLine("    > intprocan.exe /p program.txt /w 3");
            Console.WriteLine("    > intprocan.exe /p C:\\project\\program.txt /w 8 /d all");
            Console.WriteLine("    > intprocan.exe /p C:\\project\\program.txt /w 32 /d la+sa+iale");
            Console.WriteLine();
        }
    }
}
