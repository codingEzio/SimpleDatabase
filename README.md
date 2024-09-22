
## Intro

- Practice C#
- Know how a relation database like SQLite works underneath

## Feature

```bash
Available commands:
[Core]
  select       Print all rows
  insert <id> <username> <email>
[Meta]
  .exit        Exit the program
  .btree       Print the B-Tree (not yet)
  .constants   Print the constants
```

## Caveat

- Existing table definition is hardcoded and tightly coupled with the code
- The code is not optimized

## Roadmap

- [ ] Proper output the selected rows (with headers and dynamic width)
- [ ] Command that supports external input for table definition
- [ ] Syntax Highlighting (this might take a while..)

## Credit

- [Let’s Build a Simple Database](https://cstack.github.io/db_tutorial/)
