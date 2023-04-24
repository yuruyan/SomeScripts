import { fileTypeFromFile } from "file-type";
import { argv, exit } from "process";
import { readFileSync } from "fs";

if (argv.length < 3) {
  console.log(readFileSync("./Help.txt").toString());
  exit();
}
const filepath = argv[2];
// parse
try {
  console.log(await fileTypeFromFile(filepath));
} catch (error) {
  console.error(error);
}
