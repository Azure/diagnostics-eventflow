param($installPath, $toolsPath, $package, $project)
$configFile = $project.ProjectItems.Item("eventFlowConfig.json");

# Set 'Copy To Output Directory' to 'Copy if newer'
# 0: Do not copy (default, you don't need this script then)
# 1: Copy always
# 2: Copy if newer
$configFile.Properties.Item("CopyToOutputDirectory").Value=[int]2;