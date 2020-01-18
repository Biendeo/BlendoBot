import subprocess

hash = subprocess.check_output("git log --pretty=format:%h -n 1 *", shell=True).decode("utf-8").strip()
date = subprocess.check_output("git show --no-patch --no-notes --pretty=%cd", shell=True).decode("utf-8").strip()


with open("src/DecimalSpiral.cs", "r") as in_file:
	content = in_file.read()

content = content.replace("{} - {}".format(hash, date), "$VERSION")

with open("src/DecimalSpiral.cs", "w") as out_file:
	out_file.write(content)