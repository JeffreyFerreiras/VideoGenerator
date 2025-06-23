#!/usr/bin/env python3
"""
LTX Video Generator Script
Generates videos using the LTX Video model from Lightricks.
"""

import sys
import json
import os
import argparse
from pathlib import Path

def check_dependencies():
    """Check if all required dependencies are installed."""
    try:
        import torch
        import diffusers
        from diffusers import LTXPipeline, LTXImageToVideoPipeline
        from diffusers.utils import export_to_video
        
        # Check for sentencepiece which is required for T5 tokenizer
        import sentencepiece
        
        # Explicitly import transformers components to avoid lazy loading issues
        from transformers import T5Tokenizer, T5EncoderModel
        
        return True
    except ImportError as e:
        print(f"Missing dependency: {e}", file=sys.stderr)
        if "sentencepiece" in str(e).lower():
            print("ERROR: sentencepiece is required for T5 tokenizer functionality.", file=sys.stderr)
            print("Please install: pip install sentencepiece", file=sys.stderr)
        else:
            print("Please install required packages: pip install -r python_requirements.txt", file=sys.stderr)
        return False

def load_request(request_path):
    """Load generation request from JSON file."""
    try:
        with open(request_path, 'r', encoding='utf-8') as f:
            return json.load(f)
    except Exception as e:
        print(f"Error loading request file: {e}", file=sys.stderr)
        return None

def generate_video(request):
    """Generate video based on the request parameters."""
    try:
        # Import here after dependency check
        import torch
        from diffusers import LTXPipeline, LTXImageToVideoPipeline
        from diffusers.utils import export_to_video
        
        model_path = request['model_path']
        prompt = request['prompt']
        output_path = request['output_path']
        duration_seconds = request['duration_seconds']
        steps = request['steps']
        guidance_scale = request['guidance_scale']
        seed = request.get('seed')
        width = request['width']
        height = request['height']
        fps = request['fps']
        input_image = request.get('input_image')  # Optional image for image-to-video
        
        print(f"Model path: {model_path}")
        print(f"Path exists: {os.path.exists(model_path)}")
        print(f"Is file: {os.path.isfile(model_path)}")
        print(f"Is directory: {os.path.isdir(model_path)}")
        
        # Determine if model_path is a file or directory
        if os.path.isfile(model_path):
            # Single file - use from_single_file
            model_file = model_path
            model_dir = os.path.dirname(model_path)
            print(f"Loading single model file: {os.path.basename(model_file)}")
            print(f"From directory: {model_dir}")
            
            # Check if it's a .safetensors file and warn about potential issues
            if model_file.endswith('.safetensors'):
                print("WARNING: You are using a single .safetensors file.")
                print("WARNING: This may be missing required components like T5EncoderModel.")
                print("WARNING: Consider downloading the complete model directory instead.")
                print("WARNING: Use 'git clone https://huggingface.co/Lightricks/LTX-Video' for the complete model.")
            
            if not os.path.exists(model_file):
                raise FileNotFoundError(f"Model file not found: {model_file}")
        elif os.path.isdir(model_path):
            # Directory - use from_pretrained
            model_dir = model_path
            model_file = None
            print(f"Loading model from directory: {model_dir}")
            
            # Check for model_index.json
            model_index_path = os.path.join(model_dir, "model_index.json")
            if not os.path.exists(model_index_path):
                print(f"ERROR: Model directory missing model_index.json: {model_dir}")
                print("ERROR: This indicates the directory is incomplete or not a valid model directory.")
                print("ERROR: Use 'git clone https://huggingface.co/Lightricks/LTX-Video' to download the complete model.")
                raise FileNotFoundError(f"Model directory missing model_index.json: {model_dir}")
            else:
                print(f"Found model_index.json - this looks like a complete model directory")
        else:
            raise FileNotFoundError(f"Model path not found: {model_path}")
        
        # Determine which pipeline to use based on whether an image is provided
        use_image_to_video = input_image is not None and input_image.strip() != ""
        pipeline_class = LTXImageToVideoPipeline if use_image_to_video else LTXPipeline
        pipeline_name = "Image-to-Video" if use_image_to_video else "Text-to-Video"
        
        print(f"Using {pipeline_name} pipeline")
        
        # Progress: Loading model
        print("STATUS: Loading model...")
        print(f"PROGRESS: Step 1 of {steps + 3}")  # +3 for load, prepare, save steps
        sys.stdout.flush()
        
        # Load the LTX Video pipeline
        try:
            if model_file:
                # Load from single file
                pipe = pipeline_class.from_single_file(
                    model_file, 
                    torch_dtype=torch.bfloat16
                )
            else:
                # Load from directory
                pipe = pipeline_class.from_pretrained(
                    model_dir, 
                    torch_dtype=torch.bfloat16,
                    variant="fp16" if torch.cuda.is_available() else None
                )
        except Exception as e:
            error_msg = str(e)
            print(f"Error loading pipeline: {error_msg}", file=sys.stderr)
            
            # Check for specific T5 tokenizer/component loading errors
            if ("_LazyModule" in error_msg and "Placeholder" in error_msg) or "cannot be loaded" in error_msg:
                print("", file=sys.stderr)
                print("=" * 80, file=sys.stderr)
                print("CRITICAL ERROR: T5 Tokenizer/Component Loading Issue", file=sys.stderr)
                print("=" * 80, file=sys.stderr)
                print("This is likely due to missing sentencepiece dependency or transformers compatibility issue.", file=sys.stderr)
                print("", file=sys.stderr)
                print("SOLUTIONS to try:", file=sys.stderr)
                print("1. Install missing dependency:", file=sys.stderr)
                print("   pip install sentencepiece", file=sys.stderr)
                print("", file=sys.stderr)
                print("2. Update your packages:", file=sys.stderr)
                print("   pip install --upgrade transformers diffusers torch", file=sys.stderr)
                print("", file=sys.stderr)
                print("3. If using a complete model directory, ensure it contains all files:", file=sys.stderr)
                print("   - model_index.json", file=sys.stderr)
                print("   - text_encoder/ directory with T5 model files", file=sys.stderr)
                print("   - tokenizer/ directory with T5 tokenizer files", file=sys.stderr)
                print("", file=sys.stderr)
                print("4. Download the complete model if missing:", file=sys.stderr)
                print("   git clone https://huggingface.co/Lightricks/LTX-Video", file=sys.stderr)
                print("=" * 80, file=sys.stderr)
                raise RuntimeError("T5 tokenizer loading failed. Please install sentencepiece and ensure complete model directory.")
                
            # Check for specific T5EncoderModel error  
            elif "T5EncoderModel" in error_msg and "missing" in error_msg:
                print("", file=sys.stderr)
                print("=" * 80, file=sys.stderr)
                print("CRITICAL ERROR: T5EncoderModel Missing", file=sys.stderr)
                print("=" * 80, file=sys.stderr)
                print("The single .safetensors file you're using is incomplete and missing the T5EncoderModel.", file=sys.stderr)
                print("", file=sys.stderr)
                print("SOLUTION: You need to download the complete LTX-Video model directory.", file=sys.stderr)
                print("", file=sys.stderr)
                print("Method 1 - Git Clone (Recommended):", file=sys.stderr)
                print("  git clone https://huggingface.co/Lightricks/LTX-Video", file=sys.stderr)
                print("", file=sys.stderr)
                print("Method 2 - Download from Hugging Face:", file=sys.stderr)
                print("  1. Go to: https://huggingface.co/Lightricks/LTX-Video", file=sys.stderr)
                print("  2. Click 'Download repository'", file=sys.stderr)
                print("  3. Point the model path to the downloaded directory (not the .safetensors file)", file=sys.stderr)
                print("", file=sys.stderr)
                print("Method 3 - Use Hugging Face Hub:", file=sys.stderr)
                print("  pip install huggingface_hub", file=sys.stderr)
                print("  huggingface-cli download Lightricks/LTX-Video --local-dir ./LTX-Video", file=sys.stderr)
                print("", file=sys.stderr)
                print("After downloading, set your model path to the directory containing model_index.json", file=sys.stderr)
                print("=" * 80, file=sys.stderr)
                raise RuntimeError("Cannot proceed with incomplete model. Please download the complete model directory.")
            
            # Try fallback loading for other errors
            print("Attempting fallback loading without dtype specification...", file=sys.stderr)
            try:
                if model_file:
                    pipe = pipeline_class.from_single_file(model_file)
                else:
                    pipe = pipeline_class.from_pretrained(model_dir)
                print("Fallback loading successful", file=sys.stderr)
            except Exception as fallback_e:
                fallback_error_msg = str(fallback_e)
                print(f"Fallback loading also failed: {fallback_error_msg}", file=sys.stderr)
                
                # Final attempt with explicit tokenizer/text_encoder loading for T5 issues
                if ("_LazyModule" in fallback_error_msg and "Placeholder" in fallback_error_msg) or "cannot be loaded" in fallback_error_msg:
                    print("Attempting final fallback with explicit component loading...", file=sys.stderr)
                    try:
                        from transformers import T5Tokenizer, T5EncoderModel
                        
                        if model_file:
                            print("ERROR: Cannot use explicit component loading with single file.", file=sys.stderr)
                            print("ERROR: Please use complete model directory instead.", file=sys.stderr)
                            raise RuntimeError("Cannot use explicit component loading with single file. Please use complete model directory.")
                        
                        # Try to load components explicitly
                        tokenizer_path = os.path.join(model_dir, "tokenizer")
                        text_encoder_path = os.path.join(model_dir, "text_encoder")
                        
                        if not os.path.exists(tokenizer_path):
                            raise FileNotFoundError(f"Tokenizer directory not found: {tokenizer_path}")
                        if not os.path.exists(text_encoder_path):
                            raise FileNotFoundError(f"Text encoder directory not found: {text_encoder_path}")
                        
                        print(f"Loading tokenizer from: {tokenizer_path}", file=sys.stderr)
                        tokenizer = T5Tokenizer.from_pretrained(tokenizer_path)
                        
                        print(f"Loading text encoder from: {text_encoder_path}", file=sys.stderr)
                        text_encoder = T5EncoderModel.from_pretrained(text_encoder_path)
                        
                        print("Loading pipeline with explicit components...", file=sys.stderr)
                        pipe = pipeline_class.from_pretrained(
                            model_dir,
                            tokenizer=tokenizer,
                            text_encoder=text_encoder
                        )
                        print("Explicit component loading successful", file=sys.stderr)
                    except Exception as final_e:
                        print(f"Final fallback also failed: {final_e}", file=sys.stderr)
                        print("", file=sys.stderr)
                        print("All loading attempts failed. This suggests:", file=sys.stderr)
                        print("1. Missing sentencepiece dependency: pip install sentencepiece", file=sys.stderr)
                        print("2. Incompatible package versions", file=sys.stderr)
                        print("3. Incomplete model directory", file=sys.stderr)
                        raise RuntimeError(f"All loading attempts failed. Original error: {error_msg}")
                else:
                    raise
        
        # Progress: Model loaded, preparing for generation
        print("STATUS: Model loaded successfully")
        print(f"PROGRESS: Step 2 of {steps + 3}")
        sys.stdout.flush()
        
        # Move to GPU if available
        if torch.cuda.is_available():
            pipe = pipe.to('cuda')
            print(f"Using CUDA acceleration (GPU: {torch.cuda.get_device_name()})")
            print(f"Available VRAM: {torch.cuda.get_device_properties(0).total_memory // 1024**3} GB")
        else:
            print("Using CPU (this will be significantly slower)")
        
        # Set seed for reproducibility
        if seed is not None:
            torch.manual_seed(seed)
            if torch.cuda.is_available():
                torch.cuda.manual_seed(seed)
            print(f"Using seed: {seed}")
        
        print(f"Generating video with prompt: '{prompt}'")
        print(f"Parameters: {width}x{height}, {duration_seconds}s @ {fps}fps, {steps} steps")
        
        # Calculate number of frames
        num_frames = duration_seconds * fps
        print(f"Total frames to generate: {num_frames}")
        
        # Load input image if provided
        image = None
        if use_image_to_video:
            from PIL import Image
            print(f"Loading input image: {input_image}")
            if os.path.exists(input_image):
                image = Image.open(input_image).convert('RGB')
                print(f"Loaded image: {image.size}")
            else:
                raise FileNotFoundError(f"Input image not found: {input_image}")
        
        # Progress: Starting generation
        print("STATUS: Starting video generation...")
        print(f"PROGRESS: Step 3 of {steps + 3}")
        sys.stdout.flush()
        
        # Generate video
        try:
            # Note: callback parameter removed due to LTX pipeline compatibility
            # Progress will be tracked through intermediate status updates
            
            if use_image_to_video:
                result = pipe(
                    image=image,
                    prompt=prompt,
                    num_inference_steps=steps,
                    guidance_scale=guidance_scale,
                    width=width,
                    height=height,
                    num_frames=num_frames
                )
            else:
                result = pipe(
                    prompt=prompt,
                    num_inference_steps=steps,
                    guidance_scale=guidance_scale,
                    width=width,
                    height=height,
                    num_frames=num_frames
                )
            
            video_frames = result.frames[0]
            
        except Exception as e:
            print(f"Error during video generation: {e}", file=sys.stderr)
            raise
        
        # Progress: Saving video
        print("STATUS: Saving video...")
        print(f"PROGRESS: Step {steps + 3} of {steps + 3}")
        sys.stdout.flush()
        
        # Ensure output directory exists
        output_dir = os.path.dirname(output_path)
        os.makedirs(output_dir, exist_ok=True)
        
        print(f"Saving video to: {output_path}")
        
        # Export to video file
        export_to_video(video_frames, output_path, fps=fps)
        
        # Verify the file was created
        if os.path.exists(output_path):
            file_size = os.path.getsize(output_path)
            print(f"Video generation completed successfully!")
            print(f"Output file: {output_path}")
            print(f"File size: {file_size / (1024*1024):.2f} MB")
            print("STATUS: Generation completed!")
            print(f"PROGRESS: Step {steps + 3} of {steps + 3}")
            sys.stdout.flush()
        else:
            raise FileNotFoundError("Video file was not created")
            
    except Exception as e:
        print(f"Error during video generation: {str(e)}", file=sys.stderr)
        raise

def main():
    """Main entry point."""
    parser = argparse.ArgumentParser(description='Generate videos using LTX Video model')
    parser.add_argument('request_file', nargs='?', help='JSON file containing generation request')
    parser.add_argument('--check-deps', action='store_true', help='Check dependencies and exit')
    
    args = parser.parse_args()
    
    # Check dependencies first
    if not check_dependencies():
        sys.exit(1)
    
    if args.check_deps:
        print("All dependencies are available")
        sys.exit(0)
    
    # If not checking deps, request_file is required
    if not args.request_file:
        parser.error("request_file is required when not using --check-deps")
    
    # Load request
    request = load_request(args.request_file)
    if request is None:
        sys.exit(1)
    
    try:
        generate_video(request)
        print("SUCCESS: Video generation completed")
    except KeyboardInterrupt:
        print("Generation cancelled by user", file=sys.stderr)
        sys.exit(130)
    except Exception as e:
        print(f"FAILED: {str(e)}", file=sys.stderr)
        sys.exit(1)

if __name__ == '__main__':
    main() 