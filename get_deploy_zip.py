from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
from shutil import copyfile

bin_targets = ['TakeHideouts.dll', '0Harmony.dll']
base_targets = ['SubModule.xml', 'Harmony_LICENSE.txt']

modules_dir_str = "Modules/"
basedir_str = "TakeHideouts/"

partial_bindir_str = "bin/Win64_Shipping_Client/"

modules = Path(modules_dir_str)
basedir = modules / basedir_str

#make directory structure
full_bindir = basedir / partial_bindir_str
full_bindir.mkdir(parents=True, exist_ok=True)

#copy targets into directory structure
bin_source_dir = Path("TakeHideouts/bin/Release")
base_source_dir = Path("TakeHideouts/")

zip_targets = []

for target in bin_targets:
  destination = full_bindir / target
  copyfile(bin_source_dir / target, destination)
  zip_targets.append(destination)
  

for target in base_targets:
  destination = basedir / target
  copyfile(base_source_dir / target, basedir / target)
  zip_targets.append(destination)

print(modules.absolute())

#zip up the directory
z = ZipFile("TakeHideouts.zip", 'w', compression=ZIP_DEFLATED)
for t in zip_targets:
    z.write(t)
z.close()