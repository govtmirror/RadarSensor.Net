import argparse
import os
import shutil

parser = argparse.ArgumentParser(description='config for WiX build')
parser.add_argument('config', type=str, help='config type for WiX build')
args = parser.parse_args()
config = args.config

solution_dir = os.path.dirname(os.path.realpath(__file__))
bin = os.path.join(solution_dir, 'bin')
config_dir = os.path.join(bin, 'config')
build_dir = os.path.join(bin, config)

def postHarvest():
    shutil.move(os.path.join(solution_dir, 'Service.exe'), build_dir)    

if __name__ == "__main__":
    postHarvest()