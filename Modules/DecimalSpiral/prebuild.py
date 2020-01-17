import subprocess

hash = subprocess.check_output("git log --pretty=format:%h -n 1 *", shell=True).decode("utf-8").strip()
date = subprocess.check_output("git show --no-patch --no-notes --pretty=%cd", shell=True).decode("utf-8").strip()

with open("version.txt", "w") as f:
	f.write("{} - {}".format(hash, date))