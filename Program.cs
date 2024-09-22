
using SimpleDatabase.Utility;

namespace SimpleDatabase;

public class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: SimpleDatabase <filename>");
            return;
        }

        using (Database db = new Database(args[0]))
        {
            Console.WriteLine("Welcome to SimpleDatabase!");
            Console.WriteLine("Available commands:");
            Console.WriteLine("[Core]");
            Console.WriteLine("  select       Print all rows");
            Console.WriteLine("  insert <id> <username> <email>");
            Console.WriteLine("[Meta]");
            Console.WriteLine("  .exit        Exit the program");
            Console.WriteLine("  .btree       Print the B-Tree (not yet)");
            Console.WriteLine("  .constants   Print the constants");

            while (true)
            {
                Console.Write("db > ");
                string input = Console.ReadLine().Trim();

                if (string.IsNullOrEmpty(input))
                {
                    continue;
                }

                if (input.StartsWith("."))
                {
                    switch (input.ToLower())
                    {
                        case ".exit":
                            break;

                        case ".btree":
                            db.PrintTree();

                            break;

                        case ".constants":
                            db.PrintConstants();

                            break;

                        default:
                            Console.WriteLine($"Unknown meta command: {input}");
                            break;
                    }
                }
                else
                {
                    string[] parts = input.Split(' ', 4, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 0)
                    {
                        Console.WriteLine("Empty input");
                        continue;
                    }

                    string command = parts[0].ToLower();

                    try
                    {
                        switch (command)
                        {
                            case "insert":
                                if (parts.Length != 4)
                                {
                                    Console.WriteLine($"Syntax error: insert <id> <username> <email>");
                                }
                                else
                                {
                                    if (!uint.TryParse(parts[1], out uint id))
                                    {
                                        Console.WriteLine("ID must be a non-negative unsigned integer");
                                    }

                                    string username = parts[2];
                                    string email = parts[3];

                                    db.Insert(id, username, email);
                                    Console.WriteLine($"Row inserted ({id}, {username}, {email})");
                                }

                                break;

                            case "select":
                                var rows = db.Select();

                                foreach (var row in rows)
                                {
                                    Console.WriteLine($"{row.Id}, {row.Username}, {row.Email}");
                                }

                                Console.WriteLine("Query executed");

                                break;

                            default:
                                Console.WriteLine($"Unknown command: {command}");
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error: {e.Message}");
                    }
                }
            }

        }
    }
}